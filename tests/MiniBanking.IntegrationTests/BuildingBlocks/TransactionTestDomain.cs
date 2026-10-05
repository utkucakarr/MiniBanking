using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniBanking.BuildingBlocks.Cqrs;
using MiniBanking.BuildingBlocks.Persistence;
using MiniBanking.SharedKernel.Results;
using Npgsql;

namespace MiniBanking.IntegrationTests.BuildingBlocks;

// A tiny fake "module" used only to test the transaction decorator against real PostgreSQL.

internal sealed class Note
{
    public Guid Id { get; init; }

    public string Text { get; set; } = string.Empty;

    public NoteTag? Tag { get; set; }

    // Mapped to PostgreSQL's xmin system column: optimistic concurrency (ADR 0004).
    public uint Version { get; set; }
}

internal sealed class NotesDbContext(DbContextOptions<NotesDbContext> options) : DbContext(options)
{
    public const string Schema = "notes";

    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<Note>().Property(note => note.Version).IsRowVersion();
        modelBuilder.Entity<Note>().Property(note => note.Tag)
            .HasValueObjectConversion(tag => tag!.Value); // EF never calls converters with null
    }
}

/// <summary>A single-value value object whose rule got stricter over time: tags must now be upper case.</summary>
internal sealed record NoteTag
{
    private NoteTag(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<NoteTag> Create(string value) =>
        value == value.ToUpperInvariant() ? new NoteTag(value) : NoteErrors.Rejected;
}

internal static class NoteErrors
{
    public static readonly Error Rejected = Error.BusinessRule("Notes.Rejected", "The note was rejected.");
}

/// <summary>Adds a note; when <see cref="Reject"/> is true the handler adds it but then returns a failure.</summary>
internal sealed record AddNote(string Text, bool Reject = false) : ICommand<Guid>;

internal sealed class AddNoteHandler(NotesDbContext dbContext) : ICommandHandler<AddNote, Guid>
{
    public Task<Result<Guid>> Handle(AddNote command, CancellationToken cancellationToken)
    {
        var note = new Note { Id = Guid.CreateVersion7(), Text = command.Text };
        dbContext.Notes.Add(note);

        // No SaveChanges here: the transaction decorator saves and commits.
        Result<Guid> result = command.Reject ? NoteErrors.Rejected : note.Id;
        return Task.FromResult(result);
    }
}

/// <summary>Adds a note, then crashes (a bug or an infrastructure failure).</summary>
internal sealed record AddNoteThenCrash(string Text) : ICommand;

internal sealed class AddNoteThenCrashHandler(NotesDbContext dbContext) : ICommandHandler<AddNoteThenCrash>
{
    public Task<Result> Handle(AddNoteThenCrash command, CancellationToken cancellationToken)
    {
        dbContext.Notes.Add(new Note { Id = Guid.CreateVersion7(), Text = command.Text });

        throw new InvalidOperationException("Simulated crash.");
    }
}

/// <summary>
/// Renames a note, while another "user" changes the same note in between loading and saving.
/// </summary>
internal sealed record RenameNoteWithConcurrentEdit(Guid Id, string Text) : ICommand;

internal sealed class RenameNoteWithConcurrentEditHandler(NotesDbContext dbContext, IConfiguration configuration)
    : ICommandHandler<RenameNoteWithConcurrentEdit>
{
    public async Task<Result> Handle(RenameNoteWithConcurrentEdit command, CancellationToken cancellationToken)
    {
        var note = await dbContext.Notes.SingleAsync(n => n.Id == command.Id, cancellationToken);

        // Another connection updates the row and commits: its xmin changes.
        await using (var otherUser = new NpgsqlConnection(
            configuration.GetConnectionString(PersistenceServiceCollectionExtensions.ConnectionStringName)))
        {
            await otherUser.OpenAsync(cancellationToken);
            await using var update = new NpgsqlCommand(
                "UPDATE notes.notes SET text = 'changed by someone else' WHERE id = @id", otherUser);
            update.Parameters.AddWithValue("id", command.Id);
            await update.ExecuteNonQueryAsync(cancellationToken);
        }

        note.Text = command.Text;
        return Result.Success();
    }
}

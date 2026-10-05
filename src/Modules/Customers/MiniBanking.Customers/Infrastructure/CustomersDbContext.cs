using Microsoft.EntityFrameworkCore;

namespace MiniBanking.Customers.Infrastructure;

/// <summary>
/// The Customers module's database access. It only maps tables in the "customers" schema,
/// and no other module can see it (ADR 0004).
/// </summary>
internal sealed class CustomersDbContext(DbContextOptions<CustomersDbContext> options) : DbContext(options)
{
    public const string Schema = "customers";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        // Picks up every IEntityTypeConfiguration<T> in this module (one class per aggregate).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CustomersDbContext).Assembly);
    }
}

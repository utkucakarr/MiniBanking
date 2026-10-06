using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniBanking.BuildingBlocks.Persistence;
using MiniBanking.Customers.Domain;

namespace MiniBanking.Customers.Infrastructure.Configurations;

/// <summary>How a Customer is stored: table customers.customers.</summary>
internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(customer => customer.Id);
        builder.Property(customer => customer.Id)
            .HasConversion(id => id.Value, value => new CustomerId(value));

        // A value object with several values: a complex type, its properties become columns of this table.
        builder.ComplexProperty(customer => customer.Name, name =>
        {
            name.Property(n => n.FirstName).HasColumnName("first_name").HasMaxLength(PersonName.MaxLength);
            name.Property(n => n.LastName).HasColumnName("last_name").HasMaxLength(PersonName.MaxLength);
        });

        // Single-value value objects: one column each, read back through their private constructor.
        builder.Property(customer => customer.NationalId)
            .HasValueObjectConversion(nationalId => nationalId.Value)
            .HasMaxLength(NationalId.Length)
            .IsFixedLength();

        builder.Property(customer => customer.Email)
            .HasValueObjectConversion(email => email.Value)
            .HasMaxLength(Email.MaxLength);

        builder.Property(customer => customer.PhoneNumber)
            .HasValueObjectConversion(phoneNumber => phoneNumber.Value)
            .HasMaxLength(PhoneNumber.Length);

        // Stored as text ("Pending"): readable in SQL and safe if the enum's numbers ever change.
        builder.Property(customer => customer.KycStatus).HasConversion<string>().HasMaxLength(20);

        // One customer per person. The handler checks this first; the index is the safety net for races.
        builder.HasIndex(customer => customer.NationalId).IsUnique();

        // Optimistic concurrency via PostgreSQL's xmin system column; a shadow property the domain never sees.
        builder.Property<uint>("Version").IsRowVersion();

        // Domain events are not stored in the table.
        builder.Ignore(customer => customer.DomainEvents);
    }
}

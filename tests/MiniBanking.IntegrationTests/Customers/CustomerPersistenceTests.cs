using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniBanking.Customers.Domain;
using MiniBanking.Customers.Infrastructure;
using MiniBanking.IntegrationTests.Infrastructure;

namespace MiniBanking.IntegrationTests.Customers;

/// <summary>Checks the EF Core mapping of Customer (CustomerConfiguration) against the real table.</summary>
public sealed class CustomerPersistenceTests(MiniBankingApiFactory factory) : IntegrationTest(factory)
{
    private static Customer NewCustomer(string nationalId = "10000000146") =>
        Customer.Register(
            PersonName.Create("Ali Rıza", "Yılmaz").Value,
            NationalId.Create(nationalId).Value,
            new DateOnly(1990, 5, 20),
            Email.Create("ali@example.com").Value,
            PhoneNumber.Create("+905321234567").Value,
            new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero)).Value;

    [Fact]
    public async Task Customer_is_saved_and_loaded_back_with_all_its_values()
    {
        var customer = NewCustomer();
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CustomersDbContext>();
            dbContext.Customers.Add(customer);
            await dbContext.SaveChangesAsync(CancellationToken);
        }

        // A new scope = a new DbContext, so the customer really comes from the database.
        await using var readScope = Factory.Services.CreateAsyncScope();
        var loaded = await readScope.ServiceProvider.GetRequiredService<CustomersDbContext>()
            .Customers.SingleAsync(c => c.Id == customer.Id, CancellationToken);

        loaded.Should().BeEquivalentTo(customer, options => options.Excluding(c => c.DomainEvents));
    }

    [Fact]
    public async Task Database_rejects_a_second_customer_with_the_same_national_id()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CustomersDbContext>();
        dbContext.Customers.Add(NewCustomer());
        dbContext.Customers.Add(NewCustomer());

        var act = () => dbContext.SaveChangesAsync(CancellationToken);

        // The unique index is the safety net behind the handler's own check (M11).
        await act.Should().ThrowAsync<DbUpdateException>();
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MiniBanking.IntegrationTests.Infrastructure;

namespace MiniBanking.IntegrationTests.Customers;

/// <summary>End to end: HTTP → decorators → handler → real PostgreSQL, for POST and GET /api/v1/customers.</summary>
public sealed class CustomerEndpointsTests(MiniBankingApiFactory factory) : IntegrationTest(factory)
{
    private const string CustomersUrl = "/api/v1/customers";

    // The factory's clock says 2026-01-15, so the default person (born 1990) is an adult.
    private static object ValidRequest(string nationalId = "10000000146", string dateOfBirth = "1990-05-20") => new
    {
        FirstName = "Ali",
        LastName = "Yılmaz",
        NationalId = nationalId,
        DateOfBirth = dateOfBirth,
        Email = "Ali@Example.com",
        PhoneNumber = "+905321234567"
    };

    [Fact]
    public async Task Register_returns_201_with_location_and_the_customer_can_be_read()
    {
        var response = await Client.PostAsJsonAsync(CustomersUrl, ValidRequest(), CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreatedDto>(CancellationToken);
        response.Headers.Location!.ToString().Should().Be($"{CustomersUrl}/{created!.Id}");

        // Follow the Location header, like a client would.
        var customer = await Client.GetFromJsonAsync<CustomerDto>(response.Headers.Location, CancellationToken);
        customer.Should().BeEquivalentTo(new CustomerDto(
            created.Id, "Ali", "Yılmaz", "10000000146", new DateOnly(1990, 5, 20),
            "ali@example.com", "+905321234567", "Pending", Factory.TimeProvider.GetUtcNow()));
    }

    [Fact]
    public async Task Register_returns_400_with_field_errors_for_missing_fields()
    {
        var response = await Client.PostAsJsonAsync(CustomersUrl, new { FirstName = "Ali" }, CancellationToken);

        var problem = await ShouldBeProblem(response, HttpStatusCode.BadRequest, "Validation.Failed");
        problem.GetProperty("errors").TryGetProperty("LastName", out _).Should().BeTrue();
        problem.GetProperty("errors").TryGetProperty("NationalId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Register_returns_400_for_an_invalid_national_id()
    {
        var response = await Client.PostAsJsonAsync(CustomersUrl, ValidRequest(nationalId: "12345678901"), CancellationToken);

        await ShouldBeProblem(response, HttpStatusCode.BadRequest, "Customers.InvalidNationalId");
    }

    [Fact]
    public async Task Register_returns_422_for_an_underage_customer()
    {
        // Turns 18 on 2026-01-16: one day too young on the factory's "today".
        var response = await Client.PostAsJsonAsync(CustomersUrl, ValidRequest(dateOfBirth: "2008-01-16"), CancellationToken);

        await ShouldBeProblem(response, HttpStatusCode.UnprocessableEntity, "Customers.Underage");
    }

    [Fact]
    public async Task Register_returns_409_when_the_national_id_is_already_registered()
    {
        (await Client.PostAsJsonAsync(CustomersUrl, ValidRequest(), CancellationToken))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await Client.PostAsJsonAsync(CustomersUrl, ValidRequest(), CancellationToken);

        await ShouldBeProblem(response, HttpStatusCode.Conflict, "Customers.NationalIdAlreadyRegistered");
    }

    [Fact]
    public async Task Get_returns_404_for_an_unknown_customer()
    {
        var response = await Client.GetAsync($"{CustomersUrl}/{Guid.CreateVersion7()}", CancellationToken);

        await ShouldBeProblem(response, HttpStatusCode.NotFound, "Customers.NotFound");
    }

    /// <summary>Checks the status code and the stable error code of a ProblemDetails response.</summary>
    private static async Task<JsonElement> ShouldBeProblem(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        response.StatusCode.Should().Be(status);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        problem.GetProperty("code").GetString().Should().Be(code);
        return problem;
    }

    // The test's own copies of the JSON contracts: if the API changes its shape, these tests notice.
    private sealed record CreatedDto(Guid Id);

    private sealed record CustomerDto(
        Guid Id,
        string FirstName,
        string LastName,
        string NationalId,
        DateOnly DateOfBirth,
        string Email,
        string PhoneNumber,
        string KycStatus,
        DateTimeOffset RegisteredAt);
}

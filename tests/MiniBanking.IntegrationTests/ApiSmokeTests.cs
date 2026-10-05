using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MiniBanking.IntegrationTests;

/// <summary>
/// Starts the real API in memory (no network port) and sends HTTP requests to it.
/// </summary>
public sealed class ApiSmokeTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        var response = await _client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be("Healthy");
    }

    [Fact]
    public async Task Supported_currencies_are_listed()
    {
        var currencies = await _client.GetFromJsonAsync<List<CurrencyDto>>(
            "/api/v1/reference/currencies", TestContext.Current.CancellationToken);

        currencies.Should().ContainEquivalentOf(new CurrencyDto("TRY", 2));
        currencies.Should().ContainEquivalentOf(new CurrencyDto("JPY", 0));
    }

    [Fact]
    public async Task Unknown_route_returns_404_problem_details()
    {
        var response = await _client.GetAsync("/api/v1/does-not-exist", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        problem.GetProperty("status").GetInt32().Should().Be(404);
        problem.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task OpenApi_document_is_served_in_development()
    {
        var response = await _client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .Should().Contain("/api/v1/reference/currencies");
    }

    private sealed record CurrencyDto(string Code, int MinorUnits);
}

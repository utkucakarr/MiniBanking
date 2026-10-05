using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MiniBanking.BuildingBlocks.Errors;

namespace MiniBanking.BuildingBlocks.Tests.Errors;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task Unhandled_exception_becomes_a_generic_500_without_internal_details()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddErrorHandling();
        await using var provider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = provider };
        httpContext.Response.Body = new MemoryStream();

        var handler = provider.GetRequiredService<IExceptionHandler>();
        var exception = new InvalidOperationException("Connection string 'Host=db;Password=secret' is invalid.");

        var handled = await handler.TryHandleAsync(httpContext, exception, TestContext.Current.CancellationToken);

        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(500);

        httpContext.Response.Body.Position = 0;
        var body = await new StreamReader(httpContext.Response.Body).ReadToEndAsync(TestContext.Current.CancellationToken);
        var json = JsonDocument.Parse(body).RootElement;

        json.GetProperty("title").GetString().Should().Be("An unexpected error occurred.");
        json.TryGetProperty("traceId", out _).Should().BeTrue();
        body.Should().NotContain("secret", "internal details must never reach the client");
    }
}

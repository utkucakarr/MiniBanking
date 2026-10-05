using MiniBanking.BuildingBlocks.Errors;
using MiniBanking.BuildingBlocks.Modules;
using MiniBanking.SharedKernel.Monetary;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Business modules are plugged in here, starting with Customers in Phase 1.
IModule[] modules = [];

// ---------------------------------------------------------------------------
// Services (dependency injection)
// ---------------------------------------------------------------------------
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddErrorHandling();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddModules(builder.Configuration, modules);

var app = builder.Build();

// ---------------------------------------------------------------------------
// HTTP request pipeline (middleware order matters)
// ---------------------------------------------------------------------------
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHealthChecks("/health");

app.MapGet("/api/v1/reference/currencies", () =>
        TypedResults.Ok(Currency.All.Select(currency => new CurrencyResponse(currency.Code, currency.MinorUnits))))
    .WithTags("Reference")
    .WithSummary("Lists the currencies supported by the bank.");

app.MapModules();

app.Run();

internal sealed record CurrencyResponse(string Code, int MinorUnits);

// Makes the implicit Program class visible to WebApplicationFactory<Program> in the integration tests.
public partial class Program;

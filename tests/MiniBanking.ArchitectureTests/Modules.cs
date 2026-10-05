using System.Reflection;
using MiniBanking.Customers;

namespace MiniBanking.ArchitectureTests;

/// <summary>Every business module, described by its two assemblies. Add a line when a new module arrives.</summary>
internal static class Modules
{
    public static readonly IReadOnlyList<ModuleAssemblies> All =
    [
        ModuleAssemblies.For<CustomersModule>("Customers")
    ];
}

internal sealed record ModuleAssemblies(string Name, Assembly Module, Assembly Contracts)
{
    /// <summary>Root namespace of the module, e.g. "MiniBanking.Customers".</summary>
    public string Namespace => $"MiniBanking.{Name}";

    /// <summary>
    /// Everything other modules must not touch. Listed explicitly because a plain "MiniBanking.Customers"
    /// prefix would also match the allowed "MiniBanking.Customers.Contracts".
    /// </summary>
    public string[] Internals =>
    [
        $"{Namespace}.Domain",
        $"{Namespace}.Features",
        $"{Namespace}.Infrastructure",
        $"{Namespace}.{Name}Module"
    ];

    public static ModuleAssemblies For<TModule>(string name) =>
        new(name, typeof(TModule).Assembly, Assembly.Load($"MiniBanking.{name}.Contracts"));
}

using MiniBanking.SharedKernel.Results;
using NetArchTest.Rules;

namespace MiniBanking.ArchitectureTests;

/// <summary>Rules from ADR 0001: business rules don't depend on databases or the web.</summary>
public sealed class LayerTests
{
    private static readonly string[] Frameworks =
    [
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "Microsoft.AspNetCore",
        "FluentValidation"
    ];

    [Fact]
    public void Domain_does_not_depend_on_frameworks_or_other_folders()
    {
        foreach (var module in Modules.All)
        {
            var result = Types.InAssembly(module.Module)
                .That()
                .ResideInNamespace($"{module.Namespace}.Domain")
                .ShouldNot()
                .HaveDependencyOnAny(
                [
                    .. Frameworks,
                    "MiniBanking.BuildingBlocks",
                    $"{module.Namespace}.Features",
                    $"{module.Namespace}.Infrastructure"
                ])
                .GetResult();

            result.FailingTypeNames.Should().BeNullOrEmpty($"{module.Name}.Domain must stay pure");
        }
    }

    [Fact]
    public void SharedKernel_does_not_depend_on_frameworks()
    {
        var result = Types.InAssembly(typeof(Result).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny([.. Frameworks, "MiniBanking.BuildingBlocks"])
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty("SharedKernel holds pure domain building blocks only");
    }
}

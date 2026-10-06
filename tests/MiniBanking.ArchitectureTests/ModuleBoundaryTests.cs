using MiniBanking.BuildingBlocks.Modules;
using NetArchTest.Rules;

namespace MiniBanking.ArchitectureTests;

/// <summary>Rules from ADR 0001: modules are isolated and talk to each other only through Contracts.</summary>
public sealed class ModuleBoundaryTests
{
    [Fact]
    public void Modules_do_not_depend_on_other_modules_internals()
    {
        foreach (var module in Modules.All)
        {
            var otherModulesInternals = Modules.All
                .Where(other => other != module)
                .SelectMany(other => other.Internals)
                .ToArray();

            if (otherModulesInternals.Length == 0)
                continue;

            var result = Types.InAssemblies([module.Module, module.Contracts])
                .ShouldNot()
                .HaveDependencyOnAny(otherModulesInternals)
                .GetResult();

            result.FailingTypeNames.Should().BeNullOrEmpty($"{module.Name} may only use other modules' Contracts");
        }
    }

    [Fact]
    public void Contracts_stay_free_of_infrastructure()
    {
        foreach (var module in Modules.All)
        {
            var result = Types.InAssembly(module.Contracts)
                .ShouldNot()
                .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "MiniBanking.BuildingBlocks")
                .GetResult();

            result.FailingTypeNames.Should().BeNullOrEmpty($"{module.Name}.Contracts must stay a thin public API");
        }
    }

    [Fact]
    public void Only_the_module_entry_point_is_public()
    {
        foreach (var module in Modules.All)
        {
            var result = Types.InAssembly(module.Module)
                .That()
                .ArePublic()
                .And()
                // EF Core generates migrations as public classes; they are harmless.
                .DoNotResideInNamespace($"{module.Namespace}.Infrastructure.Migrations")
                .Should()
                .ImplementInterface(typeof(IModule))
                .GetResult();

            result.FailingTypeNames.Should().BeNullOrEmpty(
                $"{module.Name} internals must be internal; other modules use its Contracts (ADR 0001)");
        }
    }
}

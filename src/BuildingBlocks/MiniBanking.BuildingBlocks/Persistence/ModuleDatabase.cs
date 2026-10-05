using System.Reflection;

namespace MiniBanking.BuildingBlocks.Persistence;

/// <summary>
/// Records which DbContext belongs to which module assembly.
/// The startup migration uses it to migrate every module's schema,
/// and the transaction decorator uses it to find the DbContext of a command's module.
/// </summary>
public sealed record ModuleDatabase(Assembly ModuleAssembly, Type DbContextType);

namespace MiniBanking.BuildingBlocks.Cqrs;

/// <summary>A request that only reads state and has no side effects (e.g. GetAccountBalance).</summary>
public interface IQuery<TResponse>;

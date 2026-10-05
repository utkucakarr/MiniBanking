namespace MiniBanking.BuildingBlocks.Cqrs;

/// <summary>A request that changes state and returns no value (e.g. FreezeAccount).</summary>
public interface ICommand;

/// <summary>A request that changes state and returns a value (e.g. OpenAccount → AccountId).</summary>
public interface ICommand<TResponse>;

using MiniBanking.SharedKernel.Results;

namespace MiniBanking.BuildingBlocks.Cqrs;

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
}

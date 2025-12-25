using Account.Command.Domain;

namespace Account.Command.Application;

public interface IEventStoreRepository
{
    Task SaveAsync(AggregateRoot aggregate, CancellationToken cancellationToken = default);
    Task<T> LoadAsync<T>(Guid aggregateId, CancellationToken cancellationToken = default) where T : AggregateRoot, new();
}

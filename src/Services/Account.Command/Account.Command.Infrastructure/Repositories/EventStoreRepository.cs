using System.Text.Json;
using Account.Command.Application;
using Account.Command.Domain;
using Account.Command.Infrastructure.Data;
using Account.Command.Infrastructure.Entities;
using Banky.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Account.Command.Infrastructure.Repositories;

public class EventStoreRepository : IEventStoreRepository
{
    private readonly EventStoreDbContext _context;

    public EventStoreRepository(EventStoreDbContext context)
    {
        _context = context;
    }

    public async Task SaveAsync(AggregateRoot aggregate, CancellationToken cancellationToken = default)
    {
        var events = aggregate.GetUncommittedEvents();

        foreach (var @event in events)
        {
            var eventEntity = new EventEntity
            {
                Id = Guid.NewGuid(),
                AggregateId = aggregate.Id,
                EventType = @event.GetType().AssemblyQualifiedName ?? @event.GetType().Name,
                Data = JsonSerializer.Serialize(@event),
                OccurredOn = DateTime.UtcNow,
                Version = aggregate.Version + 1
            };
        }
        
        var version = aggregate.Version;

        foreach (var @event in events)
        {
            version++;
            var eventEntity = new EventEntity
            {
                Id = Guid.NewGuid(),
                AggregateId = aggregate.Id,
                EventType = @event.GetType().AssemblyQualifiedName ?? @event.GetType().Name,
                Data = JsonSerializer.Serialize(@event),
                OccurredOn = DateTime.UtcNow,
                Version = version
            };
            _context.Events.Add(eventEntity);
        }

        // Snapshotting every 5 versions
        if (version % 5 == 0)
        {
            var snapshot = aggregate.GetSnapshot();
            if (snapshot != null)
            {
                var snapshotEntity = new SnapshotEntity
                {
                    AggregateId = aggregate.Id,
                    Version = version,
                    CreatedOn = DateTime.UtcNow,
                    AggregateType = aggregate.GetType().Name,
                    Data = JsonSerializer.Serialize(snapshot)
                };
                
                var existingWrapper = await _context.Snapshots.FindAsync(aggregate.Id);
                if (existingWrapper != null)
                {
                    existingWrapper.Version = version;
                    existingWrapper.Data = snapshotEntity.Data;
                    existingWrapper.CreatedOn = DateTime.UtcNow;
                }
                else
                {
                    _context.Snapshots.Add(snapshotEntity);
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        aggregate.ClearEvents();
    }

    public async Task<T> LoadAsync<T>(Guid aggregateId, CancellationToken cancellationToken = default) where T : AggregateRoot, new()
    {
        var aggregate = new T();
        var snapshotType = aggregate.GetSnapshotType();
        
        int startVersion = 0;

        if (snapshotType != null)
        {
            var snapshotEntity = await _context.Snapshots.FindAsync(new object[] { aggregateId }, cancellationToken);
            if (snapshotEntity != null)
            {
                var snapshot = JsonSerializer.Deserialize(snapshotEntity.Data, snapshotType);
                if (snapshot != null)
                {
                    aggregate.HydrateFromSnapshot(snapshot, snapshotEntity.Version);
                    startVersion = snapshotEntity.Version;
                }
            }
        }

        var eventEntities = await _context.Events
            .Where(e => e.AggregateId == aggregateId && e.Version > startVersion)
            .OrderBy(e => e.Version)
            .ToListAsync(cancellationToken);

        if (startVersion == 0 && !eventEntities.Any())
        {
            return null;
        }

        var events = new List<object>();

        foreach (var entity in eventEntities)
        {
            var type = Type.GetType(entity.EventType);
            if (type != null)
            {
                var @event = JsonSerializer.Deserialize(entity.Data, type);
                if (@event != null)
                {
                    events.Add(@event);
                }
            }
        }

        aggregate.LoadFromHistory(events);
        return aggregate;
    }
}

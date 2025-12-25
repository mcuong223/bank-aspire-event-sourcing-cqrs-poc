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

        await _context.SaveChangesAsync(cancellationToken);
        aggregate.ClearEvents();
    }

    public async Task<T> LoadAsync<T>(Guid aggregateId, CancellationToken cancellationToken = default) where T : AggregateRoot, new()
    {
        var eventEntities = await _context.Events
            .Where(e => e.AggregateId == aggregateId)
            .OrderBy(e => e.Version)
            .ToListAsync(cancellationToken);

        if (!eventEntities.Any())
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

        var aggregate = new T();
        aggregate.LoadFromHistory(events);
        return aggregate;
    }
}

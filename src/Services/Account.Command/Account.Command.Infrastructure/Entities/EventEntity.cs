namespace Account.Command.Infrastructure.Entities;

public class EventEntity
{
    public Guid Id { get; set; }
    public Guid AggregateId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public DateTime OccurredOn { get; set; }
    public int Version { get; set; }
}

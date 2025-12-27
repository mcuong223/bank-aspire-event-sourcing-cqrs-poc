namespace Account.Command.Domain;

public abstract class AggregateRoot
{
    private readonly List<object> _uncommittedEvents = new();

    public Guid Id { get; protected set; }
    public int Version { get; protected set; }

    public IEnumerable<object> GetUncommittedEvents()
    {
        return _uncommittedEvents.AsReadOnly();
    }

    public void ClearEvents()
    {
        _uncommittedEvents.Clear();
    }

    public void LoadFromHistory(IEnumerable<object> history)
    {
        foreach (var e in history)
        {
            ApplyChange(e, isNew: false);
        }
    }

    protected void ApplyChange(object @event)
    {
        ApplyChange(@event, isNew: true);
    }

    private void ApplyChange(object @event, bool isNew)
    {
        Apply(@event);
        if (isNew)
        {
            _uncommittedEvents.Add(@event);
        }
        else
        {
            Version++;
        }
    }

    protected abstract void Apply(object @event);

    public virtual object? GetSnapshot() => null;
    public virtual void LoadSnapshot(object snapshot) { }
    public virtual Type? GetSnapshotType() => null;

    public void HydrateFromSnapshot(object snapshot, int version)
    {
        LoadSnapshot(snapshot);
        Version = version;
    }
}

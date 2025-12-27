namespace Account.Command.Application;

public interface IEventPublisher
{
    Task PublishAsync(object @event, CancellationToken cancellationToken = default);
}

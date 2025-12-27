using Account.Command.Application;
using Banky.Contracts;
using MassTransit;

namespace Account.Command.Infrastructure.Services;

public class KafkaEventPublisher(
    ITopicProducer<FundsDeposited> depositedProducer,
    ITopicProducer<FundsWithdrawn> withdrawnProducer) : IEventPublisher
{
    public async Task PublishAsync(object @event, CancellationToken cancellationToken = default)
    {
        if (@event is FundsDeposited deposited)
        {
            await depositedProducer.Produce(deposited, cancellationToken);
        }
        else if (@event is FundsWithdrawn withdrawn)
        {
            await withdrawnProducer.Produce(withdrawn, cancellationToken);
        }
    }
}

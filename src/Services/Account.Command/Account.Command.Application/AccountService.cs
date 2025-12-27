using Account.Command.Domain;
using MassTransit;

namespace Account.Command.Application;

public class AccountService
{
    private readonly IEventStoreRepository _repository;
    private readonly IEventPublisher _publisher;

    public AccountService(IEventStoreRepository repository, IEventPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task DepositAsync(Guid accountId, decimal amount)
    {
        var account = await _repository.LoadAsync<AccountAggregate>(accountId);
        if (account == null)
        {
             account = new AccountAggregate(accountId);
        }

        account.Deposit(amount);

        // Capture events before saving (which might clear them)
        var events = account.GetUncommittedEvents().ToList();

        await _repository.SaveAsync(account);

        foreach (var @event in events)
        {
            await _publisher.PublishAsync(@event);
        }
    }

    public async Task WithdrawAsync(Guid accountId, decimal amount)
    {
        var account = await _repository.LoadAsync<AccountAggregate>(accountId);
        if (account == null)
        {
            throw new InvalidOperationException("Account not found");
        }

        account.Withdraw(amount);

        var events = account.GetUncommittedEvents().ToList();

        await _repository.SaveAsync(account);

        foreach (var @event in events)
        {
             await _publisher.PublishAsync(@event);
        }
    }
}

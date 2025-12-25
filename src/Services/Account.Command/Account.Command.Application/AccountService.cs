using Account.Command.Domain;
using MassTransit;

namespace Account.Command.Application;

public class AccountService
{
    private readonly IEventStoreRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;

    public AccountService(IEventStoreRepository repository, IPublishEndpoint publishEndpoint)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
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
            await _publishEndpoint.Publish(@event);
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
            await _publishEndpoint.Publish(@event);
        }
    }
}

using Banky.Contracts;

namespace Account.Command.Domain;

public class AccountAggregate : AggregateRoot
{
    public decimal Balance { get; private set; }

    public AccountAggregate()
    {
        // Required for creating from history or new without Id initially
    }

    public AccountAggregate(Guid id)
    {
        Id = id;
    }

    public void Deposit(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Deposit amount must be positive");
        }

        ApplyChange(new FundsDeposited(Id, amount, DateTime.UtcNow));
    }

    public void Withdraw(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Withdraw amount must be positive");
        }

        if (Balance < amount)
        {
            throw new InvalidOperationException("Insufficient funds");
        }

        ApplyChange(new FundsWithdrawn(Id, amount, DateTime.UtcNow));
    }

    protected override void Apply(object @event)
    {
        switch (@event)
        {
            case FundsDeposited deposited:
                Id = deposited.AccountId;
                Balance += deposited.Amount;
                break;
            case FundsWithdrawn withdrawn:
                Id = withdrawn.AccountId;
                Balance -= withdrawn.Amount;
                break;
        }
    }

    public record AccountSnapshot(Guid Id, decimal Balance);

    public override object? GetSnapshot() => new AccountSnapshot(Id, Balance);

    public override void LoadSnapshot(object snapshot)
    {
        if (snapshot is AccountSnapshot s)
        {
            Id = s.Id;
            Balance = s.Balance;
        }
    }

    public override Type? GetSnapshotType() => typeof(AccountSnapshot);
}

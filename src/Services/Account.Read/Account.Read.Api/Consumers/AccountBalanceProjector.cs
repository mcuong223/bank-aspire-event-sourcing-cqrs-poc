using Account.Read.Core;
using Account.Read.Infrastructure;
using Banky.Contracts;
using MassTransit;

namespace Account.Read.Api.Consumers;

public class AccountBalanceProjector : 
    IConsumer<FundsDeposited>,
    IConsumer<FundsWithdrawn>
{
    private readonly ReadDbContext _context;
    private readonly ILogger<AccountBalanceProjector> _logger;

    public AccountBalanceProjector(ReadDbContext context, ILogger<AccountBalanceProjector> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<FundsDeposited> context)
    {
        _logger.LogInformation("Projecting Deposit Balance: {Amount} for Account {AccountId}", context.Message.Amount, context.Message.AccountId);

        var account = await _context.Accounts.FindAsync(context.Message.AccountId);
        if (account == null)
        {
            account = new AccountView { Id = context.Message.AccountId, Balance = 0, Version = 0 };
            _context.Accounts.Add(account);
        }

        account.Balance += context.Message.Amount;
        account.Version++;

        await _context.SaveChangesAsync();
    }

    public async Task Consume(ConsumeContext<FundsWithdrawn> context)
    {
        _logger.LogInformation("Projecting Withdrawal Balance: {Amount} for Account {AccountId}", context.Message.Amount, context.Message.AccountId);

        var account = await _context.Accounts.FindAsync(context.Message.AccountId);
        if (account != null)
        {
            account.Balance -= context.Message.Amount;
            account.Version++;
            await _context.SaveChangesAsync();
        }
        else
        {
            _logger.LogWarning("Account {AccountId} not found for withdrawal projection", context.Message.AccountId);
        }
    }
}

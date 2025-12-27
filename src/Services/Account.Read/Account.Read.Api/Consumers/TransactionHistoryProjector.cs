using Account.Read.Core;
using Account.Read.Infrastructure;
using Banky.Contracts;
using MassTransit;

namespace Account.Read.Api.Consumers;

public class TransactionHistoryProjector : 
    IConsumer<FundsDeposited>,
    IConsumer<FundsWithdrawn>
{
    private readonly ReadDbContext _context;
    private readonly ILogger<TransactionHistoryProjector> _logger;

    public TransactionHistoryProjector(ReadDbContext context, ILogger<TransactionHistoryProjector> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<FundsDeposited> context)
    {
        _logger.LogInformation("Projecting Deposit History: {Amount} for Account {AccountId}", context.Message.Amount, context.Message.AccountId);

        var transaction = new TransactionHistoryView
        {
            Id = Guid.NewGuid(),
            AccountId = context.Message.AccountId,
            Amount = context.Message.Amount,
            TransactionType = "Credit",
            Timestamp = context.Message.OccurredOn,
            ReferenceId = context.MessageId ?? Guid.NewGuid()
        };
        _context.TransactionHistory.Add(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task Consume(ConsumeContext<FundsWithdrawn> context)
    {
        _logger.LogInformation("Projecting Withdrawal History: {Amount} for Account {AccountId}", context.Message.Amount, context.Message.AccountId);

        var transaction = new TransactionHistoryView
        {
            Id = Guid.NewGuid(),
            AccountId = context.Message.AccountId,
            Amount = context.Message.Amount,
            TransactionType = "Debit",
            Timestamp = context.Message.OccurredOn,
            ReferenceId = context.MessageId ?? Guid.NewGuid()
        };
        _context.TransactionHistory.Add(transaction);
        await _context.SaveChangesAsync();
    }
}

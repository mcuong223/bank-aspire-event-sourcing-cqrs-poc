using Account.Read.Core;
using Account.Read.Infrastructure;
using Banky.Contracts;
using MassTransit;

namespace Account.Read.Api.Consumers;

public class LoyaltyProjector : 
    IConsumer<FundsDeposited>,
    IConsumer<FundsWithdrawn>
{
    private readonly ReadDbContext _context;
    private readonly ILogger<LoyaltyProjector> _logger;

    public LoyaltyProjector(ReadDbContext context, ILogger<LoyaltyProjector> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<FundsDeposited> context)
    {
        _logger.LogInformation("Projecting Deposit Loyalty: {Amount} for Account {AccountId}", context.Message.Amount, context.Message.AccountId);
        await ProjectAsync(context.Message);
        await _context.SaveChangesAsync();
    }

    public async Task Consume(ConsumeContext<FundsWithdrawn> context)
    {
        _logger.LogInformation("Projecting Withdrawal Loyalty: {Amount} for Account {AccountId}", context.Message.Amount, context.Message.AccountId);
        await ProjectAsync(context.Message);
        await _context.SaveChangesAsync();
    }

    private async Task ProjectAsync(object @event)
    {
        if (@event is FundsDeposited deposited)
        {
            await UpdateLoyaltyScoreAsync(deposited.AccountId, deposited.Amount, deposited.OccurredOn);
        }
        else if (@event is FundsWithdrawn withdrawn)
        {
             await UpdateLoyaltyScoreAsync(withdrawn.AccountId, -withdrawn.Amount, withdrawn.OccurredOn);
        }
    }

    private async Task UpdateLoyaltyScoreAsync(Guid accountId, decimal amountDelta, DateTime eventTime)
    {
        var loyalty = await _context.LoyaltyScores.FindAsync(accountId);
        if (loyalty == null)
        {
            loyalty = new LoyaltyView
            {
                AccountId = accountId,
                CurrentBalance = 0,
                AccumulatedScore = 0,
                FirstEventTimestamp = eventTime,
                LastEventTimestamp = eventTime, // Initial timestamp
                MembershipTier = MembershipTier.Standard
            };
            _context.LoyaltyScores.Add(loyalty);
        }

        // 1. Calculate Duration
        var timeDelta = eventTime - loyalty.LastEventTimestamp;
        
        // 2. Accrue Score
        if (timeDelta.TotalDays > 0)
        {
             loyalty.AccumulatedScore += loyalty.CurrentBalance * (decimal)timeDelta.TotalDays;
        }

        // 3. Update Balance
        loyalty.CurrentBalance += amountDelta;

        // 4. Update Timestamp
        loyalty.LastEventTimestamp = eventTime;

        // 5. Determine Tier
        var totalDaysActive = (eventTime - loyalty.FirstEventTimestamp).TotalDays;
        
        if (totalDaysActive > 0)
        {
            var averageBalance = loyalty.AccumulatedScore / (decimal)totalDaysActive;

            if (averageBalance > 5000)
            {
                loyalty.MembershipTier = MembershipTier.Platinum;
            }
            else if (averageBalance > 1000)
            {
                loyalty.MembershipTier = MembershipTier.Gold;
            }
            else
            {
                loyalty.MembershipTier = MembershipTier.Standard;
            }
        }
    }
}

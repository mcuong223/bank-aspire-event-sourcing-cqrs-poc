using Account.Read.Api.Consumers;
using Account.Read.Core;
using Account.Read.Infrastructure;
using Banky.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Account.Read.Tests;

public class LoyaltyProjectorTests
{
    private readonly ReadDbContext _context;
    private readonly Mock<ILogger<LoyaltyProjector>> _loggerMock;
    private readonly LoyaltyProjector _projector;

    public LoyaltyProjectorTests()
    {
        var options = new DbContextOptionsBuilder<ReadDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ReadDbContext(options);
        _loggerMock = new Mock<ILogger<LoyaltyProjector>>();
        _projector = new LoyaltyProjector(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task FundsDeposited_ShouldUpdateLoyaltyScore_And_DetermineTier()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var initialDeposit = 1000m;
        var firstEventTime = DateTime.UtcNow.AddDays(-10);
        
        // Act 1: Initial Deposit
        var message1 = new FundsDeposited(accountId, initialDeposit, firstEventTime);
        var contextMock1 = new Mock<ConsumeContext<FundsDeposited>>();
        contextMock1.Setup(x => x.Message).Returns(message1);
        await _projector.Consume(contextMock1.Object);

        var loyalty = await _context.LoyaltyScores.FindAsync(accountId);
        Assert.NotNull(loyalty);
        Assert.Equal(0, loyalty.AccumulatedScore); 
        Assert.Equal(initialDeposit, loyalty.CurrentBalance);

        // Act 2: Second Deposit 10 days later
        var secondDeposit = 500m;
        var secondEventTime = DateTime.UtcNow; 
        var message2 = new FundsDeposited(accountId, secondDeposit, secondEventTime);
        var contextMock2 = new Mock<ConsumeContext<FundsDeposited>>();
        contextMock2.Setup(x => x.Message).Returns(message2);
        
        await _projector.Consume(contextMock2.Object);

        // Assert
        loyalty = await _context.LoyaltyScores.FindAsync(accountId);
        // Score: 1000 * 10 = 10000.
        // Days: 10. Avg: 1000. Tier: Standard (Since requirement is > 1000)
        Assert.Equal(MembershipTier.Standard, loyalty!.MembershipTier);
    }
    
    [Fact]
    public async Task LoyaltyScore_ShouldCalculateCorrectTier_Platinum()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var initialDeposit = 6000m;
        var firstEventTime = DateTime.UtcNow.AddDays(-10);
        
        // Act 1
        var message1 = new FundsDeposited(accountId, initialDeposit, firstEventTime);
        var contextMock1 = new Mock<ConsumeContext<FundsDeposited>>();
        contextMock1.Setup(x => x.Message).Returns(message1);
        await _projector.Consume(contextMock1.Object);

        // Act 2
        var secondEventTime = DateTime.UtcNow;
        var message2 = new FundsDeposited(accountId, 0, secondEventTime); 
        var contextMock2 = new Mock<ConsumeContext<FundsDeposited>>();
        contextMock2.Setup(x => x.Message).Returns(message2); 
        await _projector.Consume(contextMock2.Object);

        // Assert
        var loyalty = await _context.LoyaltyScores.FindAsync(accountId);
        Assert.Equal(MembershipTier.Platinum, loyalty!.MembershipTier);
    }
}

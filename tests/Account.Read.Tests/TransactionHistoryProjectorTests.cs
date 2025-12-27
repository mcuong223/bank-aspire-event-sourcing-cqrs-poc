using Account.Read.Api.Consumers;
using Account.Read.Infrastructure;
using Banky.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Account.Read.Tests;

public class TransactionHistoryProjectorTests
{
    private readonly ReadDbContext _context;
    private readonly Mock<ILogger<TransactionHistoryProjector>> _loggerMock;
    private readonly TransactionHistoryProjector _projector;

    public TransactionHistoryProjectorTests()
    {
        var options = new DbContextOptionsBuilder<ReadDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ReadDbContext(options);
        _loggerMock = new Mock<ILogger<TransactionHistoryProjector>>();
        _projector = new TransactionHistoryProjector(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task FundsDeposited_ShouldCreateTransactionHistory()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = 100m;
        var occurredOn = DateTime.UtcNow;
        var message = new FundsDeposited(accountId, amount, occurredOn);
        var contextMock = new Mock<ConsumeContext<FundsDeposited>>();
        contextMock.Setup(x => x.Message).Returns(message);
        contextMock.Setup(x => x.MessageId).Returns(Guid.NewGuid());

        // Act
        await _projector.Consume(contextMock.Object);

        // Assert
        var transaction = await _context.TransactionHistory.FirstOrDefaultAsync(t => t.AccountId == accountId);
        Assert.NotNull(transaction);
        Assert.Equal(amount, transaction.Amount);
        Assert.Equal("Credit", transaction.TransactionType);
        Assert.Equal(occurredOn, transaction.Timestamp);
    }

    [Fact]
    public async Task FundsWithdrawn_ShouldCreateTransactionHistory()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = 50m;
        var occurredOn = DateTime.UtcNow;
        var message = new FundsWithdrawn(accountId, amount, occurredOn);
        var contextMock = new Mock<ConsumeContext<FundsWithdrawn>>();
        contextMock.Setup(x => x.Message).Returns(message);
        contextMock.Setup(x => x.MessageId).Returns(Guid.NewGuid());

        // Act
        await _projector.Consume(contextMock.Object);

        // Assert
        var transaction = await _context.TransactionHistory.FirstOrDefaultAsync(t => t.AccountId == accountId && t.TransactionType == "Debit");
        Assert.NotNull(transaction);
        Assert.Equal(amount, transaction.Amount);
        Assert.Equal("Debit", transaction.TransactionType);
    }
}

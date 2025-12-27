using Account.Read.Api.Consumers;
using Account.Read.Core;
using Account.Read.Infrastructure;
using Banky.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Account.Read.Tests;

public class AccountBalanceProjectorTests
{
    private readonly ReadDbContext _context;
    private readonly Mock<ILogger<AccountBalanceProjector>> _loggerMock;
    private readonly AccountBalanceProjector _projector;

    public AccountBalanceProjectorTests()
    {
        var options = new DbContextOptionsBuilder<ReadDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ReadDbContext(options);
        _loggerMock = new Mock<ILogger<AccountBalanceProjector>>();
        _projector = new AccountBalanceProjector(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task FundsDeposited_ShouldUpdateAccountBalance()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = 100m;
        var occurredOn = DateTime.UtcNow;
        var message = new FundsDeposited(accountId, amount, occurredOn);
        var contextMock = new Mock<ConsumeContext<FundsDeposited>>();
        contextMock.Setup(x => x.Message).Returns(message);

        // Act
        await _projector.Consume(contextMock.Object);

        // Assert
        var account = await _context.Accounts.FindAsync(accountId);
        Assert.NotNull(account);
        Assert.Equal(amount, account.Balance);
    }

    [Fact]
    public async Task FundsWithdrawn_ShouldUpdateAccountBalance()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var initialAmount = 200m;
        var withdrawAmount = 50m;
        var occurredOn = DateTime.UtcNow;

        // Seed
        _context.Accounts.Add(new AccountView { Id = accountId, Balance = initialAmount, Version = 1 });
        await _context.SaveChangesAsync();

        var message = new FundsWithdrawn(accountId, withdrawAmount, occurredOn);
        var contextMock = new Mock<ConsumeContext<FundsWithdrawn>>();
        contextMock.Setup(x => x.Message).Returns(message);

        // Act
        await _projector.Consume(contextMock.Object);

        // Assert
        var account = await _context.Accounts.FindAsync(accountId);
        Assert.NotNull(account);
        Assert.Equal(initialAmount - withdrawAmount, account.Balance);
    }
}

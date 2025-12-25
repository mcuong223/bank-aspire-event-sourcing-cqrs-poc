using Account.Command.Domain;
using Banky.Contracts;
using FluentAssertions;
using Xunit;

namespace Account.Command.Domain.Tests;

public class AccountAggregateTests
{
    [Fact]
    public void Deposit_ShouldApplyFundsDeposited_And_UpdateBalance()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var sut = new AccountAggregate(accountId);
        var amount = 100m;

        // Act
        sut.Deposit(amount);

        // Assert
        sut.Balance.Should().Be(amount);
        sut.Id.Should().Be(accountId);

        var events = sut.GetUncommittedEvents().ToList();
        events.Should().HaveCount(1);
        var @event = events.First().Should().BeOfType<FundsDeposited>().Subject;
        @event.AccountId.Should().Be(accountId);
        @event.Amount.Should().Be(amount);
        @event.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Withdraw_ShouldApplyFundsWithdrawn_And_DecreaseBalance()
    {
        // Arrange
        var sut = new AccountAggregate(Guid.NewGuid());
        sut.Deposit(100m); // Balance = 100
        sut.ClearEvents(); // Clear deposit event to focus on withdraw

        // Act
        sut.Withdraw(40m);

        // Assert
        sut.Balance.Should().Be(60m);
        var events = sut.GetUncommittedEvents().ToList();
        events.Should().HaveCount(1);
        var @event = events.First().Should().BeOfType<FundsWithdrawn>().Subject;
        @event.Amount.Should().Be(40m);
    }

    [Fact]
    public void Withdraw_ShouldThrow_WhenInsufficientFunds()
    {
        // Arrange
        var sut = new AccountAggregate(Guid.NewGuid());
        sut.Deposit(50m);

        // Act
        var act = () => sut.Withdraw(100m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Insufficient funds");
    }
}

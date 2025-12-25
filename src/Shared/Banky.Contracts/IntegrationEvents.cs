namespace Banky.Contracts;

public record FundsDeposited(Guid AccountId, decimal Amount, DateTime OccurredOn);

public record FundsWithdrawn(Guid AccountId, decimal Amount, DateTime OccurredOn);

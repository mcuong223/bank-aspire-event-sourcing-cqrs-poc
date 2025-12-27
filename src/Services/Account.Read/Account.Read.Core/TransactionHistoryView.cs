namespace Account.Read.Core;

public class TransactionHistoryView
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Guid ReferenceId { get; set; }
}

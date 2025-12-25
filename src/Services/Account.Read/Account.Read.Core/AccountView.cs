namespace Account.Read.Core;

public class AccountView
{
    public Guid Id { get; set; }
    public decimal Balance { get; set; }
    public int Version { get; set; }
}

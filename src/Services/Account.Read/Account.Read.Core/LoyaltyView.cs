namespace Account.Read.Core;

public class LoyaltyView
{
    public Guid AccountId { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal AccumulatedScore { get; set; }
    public DateTime FirstEventTimestamp { get; set; }
    public DateTime LastEventTimestamp { get; set; }
    public MembershipTier MembershipTier { get; set; }
}

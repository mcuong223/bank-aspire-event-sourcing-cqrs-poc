using System.ComponentModel.DataAnnotations;

namespace Account.Command.Infrastructure.Entities;

public class SnapshotEntity
{
    [Key]
    public Guid AggregateId { get; set; }
    public int Version { get; set; }
    public string Data { get; set; } = string.Empty;
    public string AggregateType { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
}

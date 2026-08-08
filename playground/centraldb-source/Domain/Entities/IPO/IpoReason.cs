using Domain.Common;

namespace Domain.Entities.IPO;

public class IpoReason : BaseOrgAuditEntityVersion<int>, IOrgEntity
{
    public string? ReasonCode { get; set; }
    public string? ReasonName { get; set; }
    public bool? Activated { get; set; }
    public byte? ReasonType { get; set; }
}
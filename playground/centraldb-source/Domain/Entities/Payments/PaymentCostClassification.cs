using Domain.Common;

namespace Domain.Entities.Payments;

public class PaymentCostClassification : BaseOrgAuditEntity<int>
{
    public required byte ObjectType { get; set; }
    public string? OpexCapexCode { get; set; }
    public string? Description { get; set; }
}

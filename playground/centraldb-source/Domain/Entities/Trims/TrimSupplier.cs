using Domain.Common;
using Domain.Entities.Trims;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Trims;

public class TrimSupplier : BaseEntity<int>
{
    public int TrimId { get; set; }
    public int? PartnerId { get; set; }
    public int LeadTimeWithGreigeAvailable { get; set; }
    public int LeadTimeNoGreige { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public bool? Activated { get; set; }
    public int LastApprovedRequestId { get; set; }

    [ForeignKey(nameof(LastApprovedRequestId))]
    public TrimSupplierRequest? LastApprovedRequest { get; set; }

    [ForeignKey(nameof(TrimId))]
    public TrimItem? Trim { get; set; }
}

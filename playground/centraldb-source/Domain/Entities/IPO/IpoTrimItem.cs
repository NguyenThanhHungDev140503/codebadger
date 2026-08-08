using Domain.Common;
using Domain.Entities.Configs;
using Domain.Entities.Orders;

namespace Domain.Entities.IPO;

public class IpoTrimItem : BaseOrgAuditEntity<int>
{
    public int PurchaseOrderId { get; set; }
    public string? IPOTrimItemCode { get; set; }
    public string? ItemCode { get; set; }
    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public int UnitId { get; set; }
    public string? ColorExt { get; set; }
    public string? Pantone { get; set; }
    public int? ColorId { get; set; }
    public int? SizeId { get; set; }
    public string? DimensionOfTechnical { get; set; }
    public DateTime? ExpectStockinDate { get; set; }

    public Unit? Unit { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
}

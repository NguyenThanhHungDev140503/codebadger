using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Orders;

[Table("ERP.PurchaseOrders.TechPacks")]
public class PurchaseOrderTechpack : BaseEntity<int>
{
    [Column("PurchaseOrderTechpackId")]
    public override int Id { get; set; }

    public int PurchaseOrderId { get; set; }
    public string? FileName { get; set; }
    public string? Description { get; set; }
    public string? FilePath { get; set; }

    [ForeignKey(nameof(PurchaseOrderId))]
    public PurchaseOrder? PurchaseOrder { get; set; }
}

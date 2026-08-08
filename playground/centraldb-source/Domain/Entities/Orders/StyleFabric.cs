using Domain.Common;
using Domain.Entities.Configs;
using Domain.Entities.CRM;
using Domain.Entities.Fabrics;
using Domain.Entities.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Orders;

public class PoStyleFabric : BaseOrgAuditEntity<int>
{
    public int StyleId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public int? FabricTypeId { get; set; }
    public int? FabricSupplierId { get; set; }
    public int? FabricId { get; set; }
    public int? PartnerId { get; set; }
    public int? PurStatus { get; set; }
    public string? Name { get; set; }
    public int? FabricNominationId { get; set; }
    public string? CutWidthExt { get; set; }

    [ForeignKey(nameof(StyleId))]
    public Style? Style { get; set; }

    [ForeignKey(nameof(FabricTypeId))]
    public FabricType? FabricType { get; set; }

    [ForeignKey(nameof(FabricSupplierId))]
    public FabricSupplier? FabricSupplier { get; set; }

    [ForeignKey(nameof(FabricNominationId))]
    public FabricNomination? FabricNomination { get; set; }
}
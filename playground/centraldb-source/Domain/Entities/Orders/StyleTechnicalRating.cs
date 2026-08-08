using Domain.Common;
using Domain.Entities.Configs;
using Domain.Entities.CRM;
using Domain.Entities.Fabrics;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Orders;

public class StyleTechnicalRating : BaseOrgAuditEntity<int>
{
    public int StyleTechnicalId { get; set; }
    public int? FabricTypeId { get; set; }
    public int? FabricSupplierId { get; set; }
    public int? FabricId { get; set; }
    public int? PartnerId { get; set; }
    public string? RemarkOfMer { get; set; }
    public decimal? RatingValue { get; set; }
    public decimal? Efficiency { get; set; }

    [ForeignKey(nameof(FabricTypeId))]
    public FabricType? FabricType { get; set; }

    [ForeignKey(nameof(FabricSupplierId))]
    public FabricSupplier? FabricSupplier { get; set; }

    [ForeignKey(nameof(FabricId))]
    public FabricItem? Fabric { get; set; }

    [ForeignKey(nameof(PartnerId))]
    public Partner? Partner { get; set; }
}
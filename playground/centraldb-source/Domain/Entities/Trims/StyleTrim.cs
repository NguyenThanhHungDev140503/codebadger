using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;
using Domain.Entities.Configs;

namespace Domain.Entities.Trims;

[Table("ERP.PurchaseOrders.Styles.Trims")]
public class StyleTrim : BaseOrgAuditEntityVersion<int>
{
    [Column("StyleTrimId")]
    public override int Id { get; set; }

    public int? StyleId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public int? TrimGroupId { get; set; }
    public int? NominationId { get; set; }
    public string? Name { get; set; }
    public bool? HasSubstitute { get; set; }
    public bool? DyeToMatched { get; set; }
    public string? PantoneOfFabric { get; set; }
    public string? ShadeBandName { get; set; }
    public string? DimensionExt { get; set; }
    public int? FabricTypeId { get; set; }

    [ForeignKey(nameof(FabricTypeId))]
    public FabricType? FabricType { get; set; }

    [ForeignKey(nameof(TrimGroupId))]
    public TrimGroup? TrimGroup { get; set; }

    [ForeignKey(nameof(NominationId))]
    public TrimNomination? Nomination { get; set; }
}

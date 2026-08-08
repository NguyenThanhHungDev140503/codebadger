using Domain.Common;
using Domain.Entities.Configs;
using Domain.Entities.CRM;
using Domain.Entities.Trims;
using Domain.Enums;
using Domain.Shared.Extensions;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Costing;

[Table("ERP.Costing.Sheet.Trim")]
public class TrimCostingSheet : BaseEntity<int>
{
    [Column("TrimCostingSheetId")]
    public override int Id { get; set; }

    public int CostingSheetId { get; set; }
    public int? TrimId { get; set; }
    public int? TrimGroupId { get; set; }
    public int? NominationId { get; set; }
    public int? PartnerId { get; set; }
    public int? WastageId { get; set; }
    public int? TrimSupplierRequestId { get; set; }
    public int? TrimCompositionId { get; set; }
    public int? KindOfTrimId { get; set; }
    public int? TypeOfTrimId { get; set; }
    public int? StyleTrimId { get; set; }

    public string? Dimension { get; set; }
    public decimal? RatingOfTrim { get; set; }
    public decimal? TotalQtyPerSizeAfterRating { get; set; }
    public decimal? BulkPriceToUSD { get; set; }
    public decimal? SurchargeToUSD { get; set; }
    public decimal? TotalOtherFee { get; set; }
    public decimal? TotalPrice { get; set; }
    public ShippingTypes? ShippingType { get; set; }
    public decimal? WastagePercentage { get; set; }
    public decimal? WastageOfSupplier { get; set; }

    [NotMapped]
    public string? ShippingTypeName => ShippingType?.GetDisplayName();

    [ForeignKey(nameof(TrimId))]
    public TrimItem? Trim { get; set; }

    [ForeignKey(nameof(TrimGroupId))]
    public TrimGroup? TrimGroup { get; set; }

    [ForeignKey(nameof(NominationId))]
    public TrimNomination? Nomination { get; set; }

    [ForeignKey(nameof(PartnerId))]
    public Partner? Partner { get; set; }

    [ForeignKey(nameof(WastageId))]
    public Wastage? Wastage { get; set; }

    [ForeignKey(nameof(TrimSupplierRequestId))]
    public TrimSupplierRequest? TrimSupplierRequest { get; set; }

    [ForeignKey(nameof(TrimCompositionId))]
    public TrimComposition? TrimComposition { get; set; }

    [ForeignKey(nameof(KindOfTrimId))]
    public TrimsKinds? KindOfTrim { get; set; }

    [ForeignKey(nameof(TypeOfTrimId))]
    public TrimsTypes? TypeOfTrim { get; set; }
}

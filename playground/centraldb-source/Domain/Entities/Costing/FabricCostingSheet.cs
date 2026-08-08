using Domain.Common;
using Domain.Entities.CRM;
using Domain.Entities.Fabrics;
using Domain.Enums;
using Domain.Shared.Extensions;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Costing;

[Table("ERP.Costing.Sheet.Fabric")]
public class FabricCostingSheet : BaseEntity<int>
{
    [Column("FabricCostingSheetId")]
    public override int Id { get; set; }

    public int CostingSheetId { get; set; }
    public int? FabricSupplierId { get; set; }
    public int? FabricSupplierRequestId { get; set; }
    public int? WastageId { get; set; }
    public int? PartnerId { get; set; }
    public int? StyleFabricId { get; set; }

    public string? FabricNameOfSupplier { get; set; }
    public decimal? CutWidth { get; set; }
    public decimal? RatingOfFabric { get; set; }
    public decimal? RatingUsing { get; set; }
    public decimal? TotalQtyAfterRating { get; set; }
    public decimal? BulkPriceToUSD { get; set; }
    public decimal? SurchargeToUSD { get; set; }
    public decimal? TotalOtherFee { get; set; }
    public decimal? TotalPrice { get; set; }
    public ShippingTypes? ShippingType { get; set; }
    public decimal? WastageOfFabric { get; set; }
    public decimal? WastageOfSupplier { get; set; }

    [NotMapped]
    public string? ShippingTypeName => ShippingType?.GetDisplayName();

    [ForeignKey(nameof(FabricSupplierId))]
    public FabricSupplier? FabricSupplier { get; set; }

    [ForeignKey(nameof(FabricSupplierRequestId))]
    public FabricSupplierRequest? FabricSupplierRequest { get; set; }

    [ForeignKey(nameof(WastageId))]
    public Wastage? Wastage { get; set; }

    [ForeignKey(nameof(PartnerId))]
    public Partner? Partner { get; set; }
}

using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Orders;

[Table("ERP.PurchaseOrders.Styles.Technicals.CMP.Details")]
public class StyleTechnicalCmpDetail : BaseOrgEntityVersion<int>
{
    [Column("TechnicalCmpDetailId")]
    public override int Id { get; set; }

    public int? StyleTechnicalCmpId { get; set; }
    public int? CmpOperationId { get; set; }
    public decimal? Frequency { get; set; }
    public string? LevelOfOperation { get; set; }
    public decimal? QtyOfTiming { get; set; }
    public decimal? QtyOfOperator { get; set; }
    public decimal? RatingOfProductivity { get; set; }
    public string? Remark { get; set; }
    public int? OperationTimingId { get; set; }
    public int? MachineId { get; set; }
    public int? CmpFeatureId { get; set; }
    public decimal? TotalOfTiming { get; set; }
    public int? OriginalId { get; set; }
    public decimal? Consumption { get; set; }
    public decimal? TotalOfConsumption { get; set; }
    public int? Stt { get; set; }

    [ForeignKey(nameof(StyleTechnicalCmpId))]
    public StyleTechnicalCmp? StyleTechnicalCmp { get; set; }
}

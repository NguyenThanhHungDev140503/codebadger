using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Costing;

[Table("ERP.Costing.Sheet.Treatment")]
public class TreatmentCosting : BaseEntity<int>
{
    [Column("TreatmentCostingSheetId")]
    public override int Id { get; set; }

    public int CostingSheetId { get; set; }
    public int? WastageId { get; set; }
    public int? ArtworkPositionId { get; set; }
    public int? TreatmentOnId { get; set; }

    public string? Code { get; set; }
    public string? PrtUACode { get; set; }
    public string? TreatmentTypeName { get; set; }
    public int TreatmentSection { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public int? NumberOfColor { get; set; }
    public decimal? QuantityRating { get; set; }
    public decimal? Rating { get; set; }
    public decimal? Price { get; set; }
    public decimal? Surcharge { get; set; }
    public decimal? Total { get; set; }

    [ForeignKey(nameof(WastageId))]
    public Wastage? Wastage { get; set; }

    [ForeignKey(nameof(ArtworkPositionId))]
    public ArtworkPosition? CutPanel { get; set; }

    [ForeignKey(nameof(TreatmentOnId))]
    public TreatmentOn? TreatmentOn { get; set; }
}

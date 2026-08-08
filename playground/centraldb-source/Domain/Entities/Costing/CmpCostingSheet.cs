using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Costing;

[Table("ERP.Costing.Sheet.CMP")]
public class CmpCostingSheet : BaseEntity<int>
{
    [Column("CmpCostingSheetId")]
    public override int Id { get; set; }

    public int CostingSheetId { get; set; }
    public string? RequestCode { get; set; }
    public decimal? TotalCutting { get; set; }
    public decimal? TotalNumbering { get; set; }
    public decimal? TotalPrepare { get; set; }
    public decimal? TotalSewing { get; set; }
    public decimal? TotalBartack { get; set; }
    public decimal? TotalEndLine { get; set; }
    public decimal? TotalPressing { get; set; }
    public decimal? TotalFinishing { get; set; }
    public decimal? TotalPolyBag { get; set; }
    public decimal? TotalCMP { get; set; }
    public decimal? TotalPriceCMP { get; set; }
    public decimal? TotalPriceSGA { get; set; }
}

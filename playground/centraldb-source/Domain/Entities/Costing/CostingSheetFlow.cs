using Domain.Common;
using Domain.Entities.Identity;
using Domain.Enums;
using Domain.Shared.Extensions;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Costing;

[Table("ERP.Costing.Sheets.Flows")]
public class CostingSheetFlow : BaseEntity<int>
{
    [Column("CostingSheetFlowId")]
    public override int Id { get; set; }

    public int CostingSheetId { get; set; }
    public int? UserId { get; set; }
    public DateTime? Date { get; set; }
    public CostingStatus? Status_Old { get; set; }
    public CostingStatus? Status_New { get; set; }
    public CostingFlow? Action { get; set; }
    public decimal? MarginPercent { get; set; }
    public decimal? FOB { get; set; }
    public string? Note { get; set; }

    [NotMapped]
    public string? ActionName => Action?.GetDisplayName();

    [NotMapped]
    public string? StatusOldName => Status_Old?.GetDisplayName();

    [NotMapped]
    public string? StatusNewName => Status_New?.GetDisplayName();

    [ForeignKey(nameof(UserId))]
    public AppUser? User { get; set; }
}

using Domain.Common;
using Domain.Entities.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Reporting;

[Table("Report.DateFilterOption")]
public class ReportDateFilterOption : BaseOrgAuditEntityVersion<long>
{
    public int? ReportDefinitionId { get; set; }
    public string? FieldCode { get; set; }
    public string? Label { get; set; }
    public bool? IsDefault { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? Activated { get; set; }

    [ForeignKey(nameof(ReportDefinitionId))]
    public ReportDefinition? ReportDefinition { get; set; }

    [ForeignKey(nameof(CreatorId))]
    public AppUser? CreatedByUser { get; set; }

    [ForeignKey(nameof(ModifierId))]
    public AppUser? UpdatedByUser { get; set; }
}
using Domain.Common;
using Domain.Entities.Identity;
using Domain.Enums.Reporting;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Reporting;

[Table("Report.ParameterDefinition")]
public class ReportParameterDefinition : BaseOrgAuditEntityVersion<int>
{
    public int? ReportDefinitionId { get; set; }
    public string? Field { get; set; }
    public string? Label { get; set; }
    public string? InputType { get; set; }

    public DataTypes? DataType { get; set; }

    public bool? IsRequired { get; set; }
    public bool? IsMultiple { get; set; }
    public string? OptionsProvider { get; set; }
    public string? DefaultValueJson { get; set; }
    public string? ValidationJson { get; set; }
    public int? DisplayOrder { get; set; }
    public string? Notes { get; set; }
    public bool? Activated { get; set; }

    [ForeignKey(nameof(ReportDefinitionId))]
    public ReportDefinition? ReportDefinition { get; set; }

    [ForeignKey(nameof(CreatorId))]
    public AppUser? CreatedByUser { get; set; }

    [ForeignKey(nameof(ModifierId))]
    public AppUser? UpdatedByUser { get; set; }
}
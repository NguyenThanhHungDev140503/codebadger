using Domain.Common;
using Domain.Entities.Identity;
using Domain.Enums.Reporting;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Reporting;

[Table("Report.Definition")]
public class ReportDefinition : BaseOrgAuditEntityVersion<int>
{
    public string? Code { get; set; }
    public string? Name { get; set; }

    public GroupCodes? GroupCode { get; set; }

    public string? PermissionCode { get; set; }

    /// <summary>
    /// Danh sách FileFormats enum, lưu dạng "5" hoặc "5,10".
    /// </summary>
    public string? SupportedFormats { get; set; }

    public ReportDateRangeModes? DateRangeMode { get; set; }

    public bool? Activated { get; set; }

    [ForeignKey(nameof(CreatorId))]
    public AppUser? CreatedByUser { get; set; }

    [ForeignKey(nameof(ModifierId))]
    public AppUser? UpdatedByUser { get; set; }

    public ICollection<ReportParameterDefinition> ParameterDefinitions { get; set; } = [];
    public ICollection<ReportDateFilterOption> DateFilterOptions { get; set; } = [];
}
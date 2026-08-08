using Domain.Common;
using Domain.Entities.Identity;
using Domain.Enums.Reporting;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Reporting;

[Table("Report.Schedule")]
public class ReportSchedule : BaseOrgAuditEntityVersion<int>
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? ReportDefinitionId { get; set; }

    // Output
    public FileFormats? OutputFormat { get; set; }

    // Frequency
    public FrequencyTypes? FrequencyType { get; set; }

    public string? CronExpression { get; set; }
    public TimeSpan? TimeOfDay { get; set; }
    public byte? DayOfWeek { get; set; }
    public byte? DayOfMonth { get; set; }
    public byte? MonthOfYear { get; set; }
    public string? TimeZone { get; set; }
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }

    // Date range
    public string? DateFilterField { get; set; }

    public DateRangeModes? DateRangeMode { get; set; }

    public DateTime? StaticFromDate { get; set; }
    public DateTime? StaticToDate { get; set; }

    public RelativeRangeTypes? RelativeRangeType { get; set; }

    public int? RelativeNDays { get; set; }

    // Parameters
    public string? ParametersJson { get; set; }

    // State
    public DateTime? NextRunAt { get; set; }
    public DateTime? LastRunAt { get; set; }
    public string? LastRunStatus { get; set; }
    public string? HangfireJobId { get; set; }

    public bool? Activated { get; set; }

    [ForeignKey(nameof(ReportDefinitionId))]
    public ReportDefinition? ReportDefinition { get; set; }

    [ForeignKey(nameof(CreatorId))]
    public AppUser? CreatedByUser { get; set; }

    [ForeignKey(nameof(ModifierId))]
    public AppUser? UpdatedByUser { get; set; }

    public ICollection<ReportRun> Runs { get; set; } = [];
}
using Domain.Common;
using Domain.Entities.Identity;
using Domain.Enums.Reporting;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Reporting;

[Table("Report.Run")]
public class ReportRun : BaseOrgAuditEntityVersion<int>
{
    public int? ReportScheduleId { get; set; }
    public int? ReportDefinitionId { get; set; }

    public TriggerTypes? TriggerType { get; set; }

    public int? TriggeredBy { get; set; }

    public ReportRunStatus? Status { get; set; }

    public DateTime? QueuedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ResolvedParametersJson { get; set; }

    [Column("RowCount")]
    public int? TotalRowCount { get; set; }

    public int? DurationMs { get; set; }
    public string? ErrorMessage { get; set; }
    public string? HangfireJobId { get; set; }
    public Guid? CorrelationId { get; set; }

    public bool? Activated { get; set; }

    [ForeignKey(nameof(ReportScheduleId))]
    public ReportSchedule? ReportSchedule { get; set; }

    [ForeignKey(nameof(ReportDefinitionId))]
    public ReportDefinition? ReportDefinition { get; set; }

    [ForeignKey(nameof(TriggeredBy))]
    public AppUser? TriggeredByUser { get; set; }

    [ForeignKey(nameof(CreatorId))]
    public AppUser? CreatedByUser { get; set; }

    [ForeignKey(nameof(ModifierId))]
    public AppUser? UpdatedByUser { get; set; }

    public ICollection<ReportArtifact> Artifacts { get; set; } = [];
}
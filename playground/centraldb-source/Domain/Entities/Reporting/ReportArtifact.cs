using Domain.Common;
using Domain.Entities.Identity;
using Domain.Enums.Reporting;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Reporting;

[Table("Report.Artifact")]
public class ReportArtifact : BaseOrgAuditEntityVersion<int>
{
    public int? ReportRunId { get; set; }
    public string? FileName { get; set; }
    public FileFormats? FileFormat { get; set; }
    public long? SizeBytes { get; set; }
    public string? StorageProvider { get; set; }
    public string? StorageUri { get; set; }
    public string? ContentHash { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public bool? Activated { get; set; }

    [ForeignKey(nameof(ReportRunId))]
    public ReportRun? ReportRun { get; set; }

    [ForeignKey(nameof(CreatorId))]
    public AppUser? CreatedByUser { get; set; }

    [ForeignKey(nameof(ModifierId))]
    public AppUser? UpdatedByUser { get; set; }
}
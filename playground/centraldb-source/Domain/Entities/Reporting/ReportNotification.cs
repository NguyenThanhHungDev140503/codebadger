using Domain.Common;
using Domain.Entities.Identity;
using Domain.Enums.Reporting;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Reporting;

[Table("Report.Notification")]
public class ReportNotification : BaseOrgAuditEntityVersion<int>
{
    public int? UserId { get; set; }

    public ReportNotificationTypes? Type { get; set; }

    public string? Title { get; set; }
    public string? Message { get; set; }
    public string? ActionUrl { get; set; }
    public string? PayloadJson { get; set; }
    public bool? IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public bool? Activated { get; set; }

    [ForeignKey(nameof(UserId))]
    public AppUser? User { get; set; }

    [ForeignKey(nameof(CreatorId))]
    public AppUser? CreatedByUser { get; set; }

    [ForeignKey(nameof(ModifierId))]
    public AppUser? UpdatedByUser { get; set; }
}
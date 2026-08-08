using Domain.Common;
using Domain.Entities.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Configs;

public class StyleCategory : BaseOrgAuditEntityVersion<int>
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public int? ParentId { get; set; }
    public int? LeadTime { get; set; }
    public int? StandardPCS { get; set; }

    [ForeignKey(nameof(ParentId))]
    public StyleCategory? Parent { get; set; }

    [ForeignKey(nameof(CreatorId))]
    public AppUser? CreatedByUser { get; set; }

    [ForeignKey(nameof(ModifierId))]
    public AppUser? UpdatedByUser { get; set; }
}
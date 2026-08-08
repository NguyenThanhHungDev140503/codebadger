using Domain.Common;
using Domain.Entities.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Configs;

public class Season : BaseOrgAuditEntityVersion<int>
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public int? ParentId { get; set; }

    [ForeignKey(nameof(ParentId))]
    public Season? Parent { get; set; }

    [ForeignKey(nameof(CreatorId))]
    public AppUser? CreatedByUser { get; set; }

    [ForeignKey(nameof(ModifierId))]
    public AppUser? UpdatedByUser { get; set; }
}
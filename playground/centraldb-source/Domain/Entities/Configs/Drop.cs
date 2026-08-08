using Domain.Common;
using Domain.Entities.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Configs;

public class Drop : BaseOrgAuditEntityVersion<int>
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }

    [ForeignKey("CreatorId")]
    public AppUser? CreatedByUser { get; set; }

    [ForeignKey("ModifierId")]
    public AppUser? UpdatedByUser { get; set; }
}
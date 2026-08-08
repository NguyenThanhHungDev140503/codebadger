using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Configs;

public class FabricType : BaseOrgAuditEntity<int>
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public bool? IsActivate { get; set; }
    public int? GroupFabricTypeId { get; set; }

    [ForeignKey(nameof(GroupFabricTypeId))]
    public GroupFabricType? GroupFabricType { get; set; }
}
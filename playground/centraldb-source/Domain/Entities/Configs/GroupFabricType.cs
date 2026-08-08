using Domain.Common;

namespace Domain.Entities.Configs;

public class GroupFabricType : BaseOrgAuditEntity<int>
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public bool? IsActivate { get; set; }
}
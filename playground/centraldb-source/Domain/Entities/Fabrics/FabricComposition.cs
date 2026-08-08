using Domain.Common;

namespace Domain.Entities.Fabrics;

public class FabricComposition : BaseOrgAuditEntity<int>
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public bool? IsActivate { get; set; }
}
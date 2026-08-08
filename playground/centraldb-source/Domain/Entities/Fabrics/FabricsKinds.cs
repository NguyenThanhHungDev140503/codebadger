using Domain.Common;

namespace Domain.Entities.Fabrics;

public class FabricsKinds: BaseOrgAuditEntity<int>
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public bool? IsActivate { get; set; }
}
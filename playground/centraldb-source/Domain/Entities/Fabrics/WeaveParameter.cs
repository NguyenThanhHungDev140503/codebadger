using Domain.Common;

namespace Domain.Entities.Fabrics;

public class WeaveParameter : BaseOrgAuditEntity<int>
{
    public string? Name { get; set; }
    public string? Code { get; set; }
}

using Domain.Common;

namespace Domain.Entities.Fabrics;

public class FabricColor : BaseOrgAuditEntity<int>
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? ColorRGB { get; set; }
    public string? NameCode { get; set; }
}
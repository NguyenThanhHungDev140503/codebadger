using Domain.Common;

namespace Domain.Entities.Trims;

public class TrimComposition : BaseOrgAuditEntityVersion<int>
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public bool? IsActivate { get; set; }
}

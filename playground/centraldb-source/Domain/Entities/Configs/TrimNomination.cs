using Domain.Common;

namespace Domain.Entities.Configs;

public class TrimNomination : BaseOrgAuditEntityVersion<int>
{
    public string? Name { get; set; }
    public bool? Activated { get; set; }
    public string? Code { get; set; }
}

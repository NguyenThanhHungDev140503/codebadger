using Domain.Common;

namespace Domain.Entities.Configs;

public class Size : BaseOrgAuditEntityVersion<int>
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public int? SizeTypeId { get; set; }
}

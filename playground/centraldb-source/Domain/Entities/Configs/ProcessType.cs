using Domain.Common;

namespace Domain.Entities.Configs;

public class ProcessType : BaseOrgAuditEntity<short>
{
    public string? Name { get; set; }
    public short? DisplayOrder { get; set; }
    public bool? ForMer{ get; set; }
    public bool? ForClient { get; set; }
}
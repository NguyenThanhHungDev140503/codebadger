using Domain.Common;

namespace Domain.Entities.Trims;

public class TrimsKinds : BaseOrgAuditEntity<int>
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public bool? IsActivate { get; set; }
    public bool? IsComsumption { get; set; }
    public bool? BreakPrint { get; set; }
    public bool? IsTrimList { get; set; }
    public bool? IsThread { get; set; }

    public ICollection<TrimsTypes> TrimsTypes { get; set; } = [];
}

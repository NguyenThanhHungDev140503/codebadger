using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Trims;

public class TrimsTypes : BaseOrgAuditEntity<int>
{
    public int? KindOfTrimId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public bool? IsActivate { get; set; }
    public bool? IsThread { get; set; }

    [ForeignKey(nameof(KindOfTrimId))]
    public TrimsKinds? TrimsKind { get; set; }
}

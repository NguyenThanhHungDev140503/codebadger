using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;

namespace Domain.Entities.Configs;

[Table("ERP.Configs.TrimGroups")]
public class TrimGroup : BaseOrgAuditEntityVersion<int>
{
    [Column("TrimGroupId")]
    public override int Id { get; set; }

    public string? Name { get; set; }
    public string? Code { get; set; }
    public bool? Activated { get; set; }
}

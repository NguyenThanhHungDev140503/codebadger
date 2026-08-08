using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Configs;

[Table("ERP.Configs.FabricNominations")]
public class FabricNomination : BaseOrgAuditEntityVersion<int>
{
    [Column("FabricNominationId")]
    public override int Id { get; set; }

    public string? Name { get; set; }
    public string? Code { get; set; }
    public bool? Activated { get; set; }
}

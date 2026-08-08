using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Costing;

[Table("ERP.Costing.Wastages")]
public class Wastage : BaseEntity<int>
{
    [Column("WastageId")]
    public override int Id { get; set; }

    public string? Name { get; set; }
}

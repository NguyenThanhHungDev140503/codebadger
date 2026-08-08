using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Costing;

[Table("ERP.Configs.TreatmentOns")]
public class TreatmentOn : BaseEntity<int>
{
    [Column("TreatmentOnId")]
    public override int Id { get; set; }

    [Column("TreatmentOnName")]
    public string? Name { get; set; }
}

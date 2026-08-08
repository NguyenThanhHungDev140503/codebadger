using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Costing;

[Table("ERP.Artwork.Position")]
public class ArtworkPosition : BaseEntity<int>
{
    [Column("ArtworkPositionId")]
    public override int Id { get; set; }

    public string? Name { get; set; }
}

using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Fabrics;

[Table("ERP.PurchaseOrders.Styles.Fabrics.Shadebands")]
public class Shadeband : BaseOrgEntityVersion<int>
{
    [Column("ShadebandId")]
    public override int Id { get; set; }

    public int? StyleFabricLabDipId { get; set; }
    public int? StyleFabricId { get; set; }
    public string? Name { get; set; }
    public string? Remark { get; set; }
    public int? OriginalId { get; set; }
}

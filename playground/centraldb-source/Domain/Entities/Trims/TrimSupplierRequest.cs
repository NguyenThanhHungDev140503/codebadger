using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Trims;

[Table("ERP.Configs.Trims.Suppliers.Requests")]
public class TrimSupplierRequest : BaseEntity<int>
{
    [Column("TrimSupplierRequestId")]
    public override int Id { get; set; }

    public string? Name { get; set; }
}

using Domain.Common;
using Domain.Entities.Warehouse;
using Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Fabrics.Inspections
{
    public class FabricAppearanceTestingLocationHistory : BaseEntity<int>
    {
        public int RollId { get; set; }
        public int? FromWarehouseLocationId { get; set; }
        public int? ToWarehouseLocationId { get; set; }
        public string? TransferReason { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int CreatedByUserId { get; set; }
        public UASubSystem SubSystem { get; set; }

        public int? FromPalletId { get; set; }
        public int? ToPalletId { get; set; }

        [ForeignKey(nameof(FromWarehouseLocationId))]
        public WarehouseLocation? FromBinLocation { get; set; }

        [ForeignKey(nameof(ToWarehouseLocationId))]
        public WarehouseLocation? ToBinLocation { get; set; }
    }
}

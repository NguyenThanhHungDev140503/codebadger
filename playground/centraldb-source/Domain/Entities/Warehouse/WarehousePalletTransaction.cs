using Domain.Common;
using Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Warehouse
{
    public class WarehousePalletTransaction : BaseEntity<int>, IOrgEntity
    {
        public int PalletId { get; set; }
        public DateTime? Date { get; set; }
        [Column("Roll_In")]
        public int QtyRollIn { get; set; }
        [Column("Roll_Out")]
        public int QtyRollOut { get; set; }
        public int ObjectId { get; set; }
        public int ObjectType { get; set; }
        public int CompanyId { get; set; }
        public UASubSystem SubSystem { get; set; }
    }
}

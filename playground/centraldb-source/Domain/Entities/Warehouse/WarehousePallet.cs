using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Warehouse
{
    public class WarehousePallet : BaseAuditEntity<int>, IOrgEntity
    {
        #region Properties
        public required string Name { get; set; }
        public bool Activated { get; set; }
        public bool ReadyToUse { get; set; }
        public string? Remark { get; set; }
        public int CompanyId { get ; set; }
        public string? QrCodePath { get; set; }
        public int? WarehouseLocationId { get; set; }
        public int? TotalRolls { get; set; }
        #endregion

        [ForeignKey(nameof(WarehouseLocationId))]
        public WarehouseLocation? WarehouseLocation { get; set; }
    }
}

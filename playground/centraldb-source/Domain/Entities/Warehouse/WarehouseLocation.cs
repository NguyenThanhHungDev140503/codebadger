using Domain.Common;
namespace Domain.Entities.Warehouse
{
    public class WarehouseLocation : BaseAuditEntity<int>, IOrgEntity
    {
        public int WarehouseId { get; set; }
        public string? BinCode { get; set; }
        public int? BinNumber { get; set; }
        public int? Shelf { get; set; }
        public string? Rack { get; set; }
        public string? Aisle { get; set; }
        public string? Zone { get; set; }
        public string? Description { get; set; }
        public bool Activated { get; set; } 
        public string? Remark { get; set; }
        public int CompanyId { get; set; }

        public Warehouse? Warehouse { get; set; }
        public WarehousePallet? Pallet { get; set; }
    }
}

using Domain.Common;

namespace Domain.Entities.Warehouse
{
    /// <summary>
    /// [WH.Items.Transactions]
    /// </summary>
    public class WarehouseTransactionItem : BaseOrgEntity<int>
    {
        public int ItemId { get; set; }
        public int UnitId { get; set; }
        public int WarehouseId { get; set; }
        
        public decimal? QuantityIn { get; set; }
        public decimal? QuantityOut { get; set; }
        public DateTime? Date { get; set; }
        public int ObjectIdMain { get; set; }
        public int ObjectTypeMain { get; set; }
        public int ObjectId { get; set; }
        public int ObjectType { get; set; }
        public int ObjectDetailId { get; set; }
        public int ObjectDetailType { get; set; }
        public string? Description { get; set; }
        public string? BillCode { get; set; }
        public int WarehouseLocationId { get; set; }
    }
}

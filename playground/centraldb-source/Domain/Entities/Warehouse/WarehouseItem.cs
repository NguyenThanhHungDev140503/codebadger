using Domain.Common;

namespace Domain.Entities.Warehouse
{
    public class WarehouseItem : BaseOrgEntity<int>, IObjectEntity
    {
        public string? ItemName { get; set; }
        public string? ItemCode { get; set; }
        public string? Batch { get; set; }
        public int ObjectId { get; set; }
        public int ObjectType { get; set; }
    }
}

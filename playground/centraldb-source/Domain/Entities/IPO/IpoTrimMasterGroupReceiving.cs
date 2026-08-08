using Domain.Common;
using Domain.Entities.Warehouse;

namespace Domain.Entities.IPO
{
    public class IpoTrimMasterGroupReceiving : BaseOrgEntity<int>
    {
        public int IPOTrimMasterGroupId { get; set; }
        public int WarehouseLocationId { get; set; }

        public IpoTrimMasterGroup? POTrimMasterGroup { get; set; }
        public WarehouseLocation? WarehouseLocation { get; set; }
    }
}
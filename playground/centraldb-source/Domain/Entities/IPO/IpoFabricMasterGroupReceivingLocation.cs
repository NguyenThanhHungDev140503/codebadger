using Domain.Common;
using Domain.Entities.Warehouse;

namespace Domain.Entities.IPO
{
    public class IpoFabricMasterGroupReceivingLocation : BaseOrgEntity<int>
    {
        public int IPOFabricMasterGroupId { get; set; }
        public int WarehouseLocationId { get; set; }

        public IpoFabricMasterGroup? IPOFabricMasterGroup { get; set; }
        public WarehouseLocation? WarehouseLocation { get; set; }
    }
}

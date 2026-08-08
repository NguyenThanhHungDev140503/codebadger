using Domain.Common;

namespace Domain.Entities.IPO
{
    public class IpoFabricMasterGroupReceiving : BaseOrgEntity<int>, IObjectEntity
    {
        public int? IPOFabricMasterGroupId { get; set; }
        public string? LOT { get; set; }
        public decimal? ReceivedQty { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public string? LocationNames { get; set; }
        public string? LocationIds { get; set; }
        public int? TotalRoll { get; set; }
        public int ObjectId { get; set; }
        public int ObjectType { get; set; }
    }
}

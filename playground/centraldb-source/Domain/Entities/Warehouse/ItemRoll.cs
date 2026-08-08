using Domain.Common;

namespace Domain.Entities.Warehouse
{
    public class ItemRoll : BaseOrgEntity<int>
    {
        public int? FabricSupplierId { get; set; }
        public int? LotReceivingId { get; set; }
        public string? LDCode { get; set; }
        public string? LDOption { get; set; }   
        public string? RollNo { get; set; }
    }
}

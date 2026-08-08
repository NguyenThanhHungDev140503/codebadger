using Domain.Common;
using Domain.Entities.IPO;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Fabrics.Inspections
{
    public class FabricInspectionRequestDetail : BaseEntity<int>
    {
        public int FabricInspectionRequestId { get; set; }
        public int LotReceivingId { get; set; }
        public DateTime? CutDate { get; set; }
        public string? RollRequest { get; set; }
        public required string RollRequestCode { get; set; }
        public decimal CutTestQty { get; set; }
        public int? TotalRollPerRequest { get; set; }
        public int? IPOFabricMasterGroupInspectionRequestIdParent { set; get; }
        public RollPosition RollPosition { get; set; } = RollPosition.Unknown;

        [ForeignKey(nameof(LotReceivingId))]
        public IpoFabricMasterGroupReceiving? FabricMasterGroupReceiving { get; set; }

        [ForeignKey(nameof(FabricInspectionRequestId))]
        public FabricInspectionRequest? FabricInspectionRequest { get; set; }
    }
}

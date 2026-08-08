using Domain.Common;

namespace Domain.Entities.Fabrics
{
    //TODO
    public class FabricMasterPO : BaseEntity<int>, IOrgEntity
    {
        public int CompanyId { get; set; }
        public byte WorkType { get; set; }
        public string? IPOFabricMasterCode { get; set; }
        public string? IPOFabricItemCodeCombined { get; set; }
        public int PartnerId { get; set; }
        public int SupplierPartnerId { get; set; }
        public int CustomerPartnerId { get; set; }
        public decimal TotalOrderQty { get; set; }
        public decimal ActualTotalQty { get; set; }
        public decimal TotalSurcharge { get; set; }
        public decimal ActualTotalAmount { get; set; }
        public decimal TotalPreArrival { get; set; }
        public string? Remark { get; set; }
        public byte IPOFabricMasterStatus { get; set; }
        public DateTime? CombinedDate { get; set; }
        public int CombinedByUserId { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? RejectedDate { get; set; }
        public string? RejectedIPOReason { get; set; }
        public string? RemoveCombineReason { get; set; }
        public DateTime? AceptedDate { get; set; }
        public int AceptedByUserId { get; set; }
        public int RejectedByUserId { get; set; }
        public int ApprovedByUserId { get; set; }
        public int SentFinanceByUserId { get; set; }
        public DateTime? SentFinanceDate { get; set; }
        public int SentPurchManagerByUserId { get; set; }
        public DateTime? SentPurchManagerDate { get; set; }
        public int Version { get; set; }
        public bool CloneOk { get; set; }
        public int IPOFabricMasterIdParent { get; set; }
        public DateTime? RemoveCombinedDate { get; set; }
        public int RemoveCombinedByUserId { get; set; }
        public bool Deposit { get; set; }
        public DateTime? DepositDate { get; set; }
    }
}

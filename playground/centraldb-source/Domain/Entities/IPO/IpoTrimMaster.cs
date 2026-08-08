using Domain.Common;
using Domain.Entities.Accountings;
using Domain.Entities.CRM;
using Domain.Entities.Identity;
using Domain.Entities.Trims;
using Domain.Enums;
using Domain.Shared.Extensions;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.IPO
{
    public class IpoTrimMaster : BaseOrgAuditEntityVersion<int>
    {
        #region Property
        [Column("IPOTrimMasterCode")]
        public string? MasterPOCode { get; set; }

        public WorkTypes WorkType { get; set; }

        [NotMapped]
        public string WorkTypeName => WorkType.GetDisplayName();

        public string? IPOTrimItemCodeCombined { get; set; }
        public int? CustomerPartnerId { get; set; }
        public int? SupplierPartnerId { get; set; }

        public int? KindOfTrimId { get; set; }
        public int? TypeOfTrimId { get; set; }
        public int? TrimCompositionId { get; set; }

        public IpoMasterStatus? IPOTrimMasterStatus { get; set; }

        [NotMapped]
        public string? IpoTrimMasterStatusName => IPOTrimMasterStatus.GetDisplayName();

        public DateTime? CombinedDate { get; set; }
        public int? CombinedByUserId { get; set; }

        public DateTime? ApprovedDate { get; set; }
        public int? ApprovedByUserId { get; set; }

        public DateTime? RejectedDate { get; set; }
        public int? RejectedByUserId { get; set; }
        public string? RejectedIPOReason { get; set; }

        public string? RemoveCombineReason { get; set; }
        public DateTime? RemoveCombinedDate { get; set; }
        public int? RemoveCombinedByUserId { get; set; }

        public DateTime? AceptedDate { get; set; }
        public int? AceptedByUserId { get; set; }

        public bool? CloneOk { get; set; }

        public int? IPOTrimMasterIdParent { get; set; }

        public DateTime? ETATrimReceivingDate { get; set; }

        public decimal? TotalOrderQty { get; set; }
        public decimal? ActualTotalQty { get; set; }
        public decimal? TotalSurcharge { get; set; }
        public decimal? ActualTotalAmount { get; set; }

        public string? Remark { get; set; }

        public int? SentPurchManagerByUserId { get; set; }
        public DateTime? SentPurchManagerDate { get; set; }

        public decimal? TotalPreArrival { get; set; }

        public int? PurchaseOrderId { get; set; }
        public DateTime? PurchaseOrderCreatedDate { get; set; }

        public string? SupplierPartnerIds { get; set; }

        public string? SeasonIds { get; set; }
        public string? SeasonCodes { get; set; }

        public string? DropCodes { get; set; }
        public string? DropIds { get; set; }

        public string? SentPurByUserIds { get; set; }
        public string? SentPurByUserNames { get; set; }

        public int? CurrencyId { get; set; }

        public string? ProcessTypeIds { get; set; }
        public string? ProcessTypeNames { get; set; }

        public int? CommercialRejectByUserId { get; set; }
        public string? CommercialRejectReason { get; set; }
        public DateTime? CommercialRejectDate { get; set; }

        public int? CommercialApprovedByUserId { get; set; }
        public DateTime? CommercialApprovedDate { get; set; }

        public DateTime? MerRejectedDate { get; set; }
        public string? MerRejectedSeason { get; set; }

        public decimal? TotalBuyQty { get; set; }

        public string? NominatedIds { get; set; }
        public string? NominatedNames { get; set; }

        public POType? POType { get; set; }
        public string? POTypeName => POType.GetDisplayName();

        public byte? SupplierOrderState { get; set; }

        public DateTime? SupplierOrderSuccessDate { get; set; }
        public DateTime? PaymentDate { get; set; }

        public decimal? TotalAmountBeforeVAT { get; set; }

        /// <summary>
        /// Computed column
        /// </summary>
        public decimal? PaidPercent { get; private set; }
        #endregion

        [ForeignKey(nameof(CurrencyId))]
        public AppCurrency? Currency { get; set; }

        [ForeignKey(nameof(CreatorId))]
        public AppUser? CreatedByUser { get; set; }

        [ForeignKey(nameof(ModifierId))]
        public AppUser? UpdatedByUser { get; set; }

        [ForeignKey(nameof(CustomerPartnerId))]
        public Partner? Customer { get; set; }

        [ForeignKey(nameof(KindOfTrimId))]
        public TrimsKinds? KindOfTrim { get; set; }

        [ForeignKey(nameof(TypeOfTrimId))]
        public TrimsTypes? TypeOfTrim { get; set; }

        [ForeignKey(nameof(TrimCompositionId))]
        public TrimComposition? TrimComposition { get; set; }
    }
}
using Domain.Common;
using Domain.Entities.Identity;
using Domain.Entities.IPO;
using Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Fabrics.Inspections
{
    /// <summary>
    /// [ERP.IPO.Fabrics.Masters.Groups.InspectionRequests.LOTs]
    /// </summary>
    public class FabricInspectionRequestLot : BaseOrgAuditEntity<int>, IFabricQaAuditable
    {
        /// <summary>
        /// IPOFabricMasterGroupInspectionRequestId
        /// </summary>
        public int FabricInspectionRequestId { get; set; }

        /// <summary>
        /// LotReceivingId
        /// </summary>
        [Column("LotReceivingId")]        
        public int FabricMasterGroupReceivingId { get; set; }

        /// <summary>
        /// IPOFabricMasterGroupInspectionRequestLOTIdParent
        /// </summary>
        public int? ParentFabricInspectionRequestLotId { get; set; }

        public int ShippingSampleRequestId { get; set; }

        public FabricInspectionCriteriaType FabricInspectionType { get; set; }

        public string? Remark { get; set; }
        public string? RemarkOfLot { get; set; }
        public string? RemarkOfQA { get; set; }

        public DateTime? PurReviewedDate { get; set; }
        public DateTime? StockInDate { get; set; }
        public bool? FinalResult { get; set; }
        public bool? CommercialPass { get; set; }
        public string? LocationIds { get; set; }
        public string? LocationNames { get; set; }
        
        public FabricInspectionLotStatus State { get; set; }
        public bool? ReviewPhycicalTest { get; set; }
        public bool? AppearanceInps { get; set; }
        public bool? PhysicalInps { get; set; }
        public int? StockInUserId { get; set; }
        public decimal? TotalReturnedNet { get; set; }

        #region QA
        public bool? SentToQALab { get; set; }
        public DateTime? SentToQALabDate { get; set; }
        public DateTime? QAReceivedDate { get; set; }
        public bool? Physical_SentToQALab { get; set; }
        public DateTime? Physical_SentToQALabDate { get; set; }
        public DateTime? QAFinishedDate { get; set; }
        public DateTime? QAReceivedCutTestDate { get; set; }
        public int? QAReceivedUserId { get; set; }
        public int? SentToQALabUserId { get; set; }
        public bool? QAReviewed { get; set; }
        public int? QAReviewedByUserId { get; set; }
        public DateTime? QAReviewedDate { get; set; }
        #endregion

        public FabricInspectionRequest? FabricInspectionRequest { get; set; }
        [ForeignKey(nameof(FabricMasterGroupReceivingId))]
        public IpoFabricMasterGroupReceiving? Receiving { get; set; }
        [ForeignKey(nameof(CreatorId))]
        public AppUser? CreatorUser { get; set; }
        [ForeignKey(nameof(ModifierId))]
        public AppUser? ModifierUser { get; set; }
        [ForeignKey(nameof(QAReviewedByUserId))]
        public AppUser? QAReviewedByUser { get; set; }
    }

    public interface IFabricQaAuditable
    {
        public bool? SentToQALab { get; set;}
        public DateTime? SentToQALabDate { get; set;}

        public DateTime? QAReceivedDate {  get; set; }
        public bool? Physical_SentToQALab { get; set; }
        public DateTime? Physical_SentToQALabDate { get; set; }

        public DateTime? QAFinishedDate { get; set; }
        public DateTime? QAReceivedCutTestDate { get; set; }

        public int? QAReceivedUserId { get; set; }
        public int? SentToQALabUserId { get; set; }

        public bool? QAReviewed { get; set; }
        public int? QAReviewedByUserId { get; set; }
        public DateTime? QAReviewedDate { get; set; }
    }
}

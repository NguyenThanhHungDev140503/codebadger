using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.IPO;
using Domain.Shared.Extensions;

namespace Domain.Entities.Fabrics.Inspections
{
    public class FabricInspectionRequest : BaseOrgAuditEntity<int>
    {
        #region Properties
        public required string RequestCode { set; get; }
        public InspectionTypes RequestType { get; set; }
        public int IpoFabricMasterGroupId { get; set; }
        public InspectionStatus Status { get; set; }
        public DateTime? CutDate { get; set; }
        public bool? SentAgaint { get; set; }
        public bool? ForPhysical { get; set; }
        public bool? ForApperance { get; set; }
        public bool? ForBasicInsp { get; set; }
        public bool? ForPrtTest { get; set; }
        public bool? ForColorFastness { get; set; }
        public DateTime? QAReceivedCutTestDate { get; set; }
        public string? LocationIds { get; set; }
        public string? LocationNames { get; set; }
        [Column("IPOFabricMasterGroupInspectionRequestIdParent")]
        public int? ParentFabricInspectionRequestId { get; set; }
        public int? TotalRollsPerRequest { get; set; }
        public int? TotalLotsPerRequest { get; set; }
        public int? SentUserId { get; set; }
        public DateTime? LastReceivedDateOfReceiving { get; set; }
        #endregion

        public string? StatusName => this.Status.GetDisplayName();
        public string? RequestTypeName => RequestType.GetDisplayName();

        [ForeignKey(nameof(IpoFabricMasterGroupId))]
        public IpoFabricMasterGroup? POFabricMasterGroup { get; set; }

        public ICollection<FabricInspectionRequestDetail> RequestDetails { get; } = [];
    }

    public enum InspectionStatus : byte
    {
        [Display(Name = "-- Inspection Type --")] Unknown = 0,
        [Display(Name = "Draft")] Draft = 1,
        [Display(Name = "Waiting")] Waiting = 2,
        [Display(Name = "Accept")] Accepted = 8,
        [Display(Name = "Finished")] Approved = 14,
        [Display(Name = "Rejected")] Rejected = 64,
    }

    public enum InspectionTypes : byte
    {
        [Display(Name = "-- Inspection Type --")]
        Unknown = 0,
        [Display(Name = "BASIC INSP")]
        BASIC_INSPECTION = 1,
        [Display(Name = "APPEARANCE INSP")]
        APPEARANCE_INSPECTION = 2,
        [Display(Name = "COLOR SHADE")]
        COLOR_SHADE = 3,
        [Display(Name = "BLANKET")]
        BLANKET = 4,
        [Display(Name = "SHIPPING SAMPLE")]
        SHIPPINGSAMPLE = 5,
        [Display(Name = "TWISTING TEST")]
        TWISTINGTEST = 6,
        [Display(Name = "PRINTING TEST")]
        PRINTING_TEST = 7,
        [Display(Name = "COLOR FASTNESS")]
        COLORFASTNESS = 8,
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;
using Domain.Entities.Configs;
using Domain.Entities.Identity;
using Domain.Shared.Extensions;

namespace Domain.Entities.Orders;

[Table("ERP.PurchaseOrders.Styles.Technicals.POMs")]
public class StyleTechnicalPom : BaseOrgAuditEntityVersion<int>
{
    [Column("StyleTechnicalPomId")]
    public override int Id { get; set; }

    public string? RequestCode { get; set; }
    public int? StyleId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public DateTime? SentTechnicalDate { get; set; }
    public DateTime? ExpectReceiveDate { get; set; }
    public int? SentToTechnicalByUserId { get; set; }
    public Status? State { get; set; }
    [NotMapped]
    public string? StatusName => State?.GetDisplayName();
    public DateTime? SentMerDate { get; set; }
    public int? SentToMerByUserId { get; set; }
    public bool? SentAgain { get; set; }
    public DateTime? TechnicalReceivedDate { get; set; }
    public int? TechnicalReceivedByUserId { get; set; }
    public string? Remark { get; set; }
    public int? TotalPOM { get; set; }
    public int? StandardSizeId { get; set; }
    public int? StandardUnitId { get; set; }
    public bool? SentToBOM { get; set; }
    public int? SizeIdMin { get; set; }
    public int? SizeIdMax { get; set; }
    public string? RejectReason { get; set; }
    public DateTime? RejectDate { get; set; }
    public int? OriginalId { get; set; }
    public DateTime? LastExportDate { get; set; }
    public int? TotalExport { get; set; }
    public bool? Deactived { get; set; }

    public Style? Style { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public AppUser? SentToTechnicalByUser { get; set; }
    public AppUser? TechnicalReceivedByUser { get; set; }
    public Size? StandardSize { get; set; }
    public Unit? StandardUnit { get; set; }

    [ForeignKey(nameof(CreatorId))]
    public AppUser? CreatedByUser { get; set; }

    [ForeignKey(nameof(ModifierId))]
    public AppUser? UpdatedByUser { get; set; }

    public enum Status : byte
    {
        [Display(Name = "N/A")] None = 0,
        [Display(Name = "Waiting")] Waiting = 2,
        [Display(Name = "Accepted")] Accepted = 8,
        [Display(Name = "Finished")] Finished = 16,
        [Display(Name = "Rejected")] Rejected = 32
    }
}

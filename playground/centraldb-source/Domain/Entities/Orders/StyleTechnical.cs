using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;
using Domain.Entities.Identity;
using Domain.Shared.Extensions;

namespace Domain.Entities.Orders;

[Table("ERP.PurchaseOrders.Styles.Technicals")]
public class StyleTechnical : BaseOrgAuditEntityVersion<int>
{
    [Column("StyleTechnicalId")]
    public override int Id { get; set; }
    public string? RequestCode { get; set; }
    public int? StyleId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public DateTime? SentTechnicalDate { get; set; }
    public int? SentToTechnicalByUserId { get; set; }
    public DateTime? ExpectReceiveDate { get; set; }
    public DateTime? ReceiveDateOfTechnical { get; set; }
    public string? Remark { get; set; }
    public StyleTechnicalPom.Status? State { get; set; }
    [NotMapped]
    public string? StatusName => State?.GetDisplayName();
    public int? StyleTechnicalRandomId { get; set; }
    public int? Stt { get; set; }
    public DateTime? SentMerDate { get; set; }
    public int? SentToMerByUserId { get; set; }
    public bool? SentAgain { get; set; }
    public int? ReceiveOfTechnicalUserId { get; set; }
    public int? StyleFabricId { get; set; }
    public string? ReasonOfReject { get; set; }
    public int? OriginalId { get; set; }
    public bool? Deactived { get; set; }
    public FabricRatingProcessBy? FabricRatingProcessBy { get; set; }
    [NotMapped]
    public string? FabricRatingProcessByName => FabricRatingProcessBy.GetDisplayName();

    public Style? Style { get; set; }

    public PurchaseOrder? PurchaseOrder { get; set; }

    public PoStyleFabric? StyleFabric { get; set; }

    public StyleTechnicalRating? StyleTechnicalRating { get; set; }

    public AppUser? SentToMerByUser { get; set; }

    [ForeignKey(nameof(SentToTechnicalByUserId))]
    public AppUser? SentToTechnicalByUser { get; set; }

    [ForeignKey(nameof(ReceiveOfTechnicalUserId))]
    public AppUser? ReceiveOfTechnicalUser { get; set; }

    [ForeignKey(nameof(CreatorId))]
    public AppUser? CreatedByUser { get; set; }

    [ForeignKey(nameof(ModifierId))]
    public AppUser? UpdatedByUser { get; set; }
}

public enum FabricRatingProcessBy : byte
{
    [Display(Name = "-- Process By --")] Unknown = 0,
    [Display(Name = "Technical")] ByTechnical = 1,
    [Display(Name = "Outsource")] ByOutsource = 2
}
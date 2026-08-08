using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;
using Domain.Entities.Identity;
using Domain.Shared.Extensions;

namespace Domain.Entities.Orders;

[Table("ERP.PurchaseOrders.Trims.Technicals")]
public class TrimTechnical : BaseOrgAuditEntity<int>
{
    [Column("TrimTechnicalId")]
    public override int Id { get; set; }
    public string? RequestCode { get; set; }
    public int? StyleId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public DateTime? SentTechnicalDate { get; set; }
    public int? SentToTechnicalByUserId { get; set; }
    public DateTime? ExpectReceiveDate { get; set; }
    public DateTime? ReceiveDateOfTechnical { get; set; }
    public int? ReceiveOfTechnicalUserId { get; set; }
    public string? Remark { get; set; }
    public StyleTechnicalPom.Status? State { get; set; }
    [NotMapped]
    public string? StatusName => State?.GetDisplayName();
    public int? TrimTechnicalRandomId { get; set; }
    public int? Stt { get; set; }
    public DateTime? SentMerDate { get; set; }
    public int? SentToMerByUserId { get; set; }
    public bool? SentAgain { get; set; }
    public InputBy? TechnicalInputBy { get; set; }
    public string? ReasonOfReject { get; set; }
    public int? OriginalId { get; set; }
    public bool? IsThread { get; set; }
    public bool? Deactived { get; set; }

    [NotMapped]
    public string? TechnicalInputByName => TechnicalInputBy.GetDisplayName();

    public Style? Style { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public AppUser? SentToTechnicalByUser { get; set; }
    public AppUser? ReceiveOfTechnicalUser { get; set; }
    public AppUser? SentToMerByUser { get; set; }

    [ForeignKey(nameof(CreatorId))]
    public AppUser? CreatedByUser { get; set; }

    [ForeignKey(nameof(ModifierId))]
    public AppUser? UpdatedByUser { get; set; }
}
public enum InputBy : byte
{
    [Display(Name = "-- Process By --")] Unknown = 0,
    [Display(Name = "Technical")] ByTechnical = 1,
    [Display(Name = "Merch")] ByMerch = 2
}

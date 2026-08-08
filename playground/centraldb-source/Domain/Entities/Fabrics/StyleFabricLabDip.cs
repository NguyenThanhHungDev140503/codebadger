using Domain.Common;
using Domain.Entities.Identity;
using Domain.Entities.Orders;
using Domain.Enums;
using Domain.Shared.Extensions;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Fabrics;

[Table("ERP.PurchaseOrders.Styles.Fabrics.LabDips")]
public class StyleFabricLabDip : BaseOrgAuditEntityVersion<int>
{
    [Column("StyleFabricLabDipId")]
    public override int Id { get; set; }

    public string? RequestCode { get; set; }
    public int StyleFabricId { get; set; }
    public int StyleId { get; set; }
    public int PurchaseOrderId { get; set; }
    public int? ShadebandId { get; set; }
    public DateTime? SentPurDate { get; set; }
    public DateTime? ExpectReceiveDate { get; set; }
    public DateTime? ReceiveDateOfPur { get; set; }
    public DateTime? PurSentMerDate { get; set; }
    public DateTime? SentCustomerDate { get; set; }
    public DateTime? CustomerApprovedDate { get; set; }
    public DateTime? CustomerRejectedDate { get; set; }
    public DateTime? CustomerCanceledDate { get; set; }
    public int? SentToPurByUserId { get; set; }
    public int? SentToMerByUserId { get; set; }
    public int? ReceivedByUserId { get; set; }
    public int? CustomerApprovedByUserId { get; set; }
    public int? CustomerRejectedByUserId { get; set; }
    public int? CustomerCanceledByUserId { get; set; }
    public StyleDevelopmentState State { get; set; }
    public StyleDevelopmentCustomerStatus CustomerStatus { get; set; }
    public string? ColorExt { get; set; }
    public string? Pantone { get; set; }
    public string? PantoneOfSupplier { get; set; }
    public string? PurRejectedReason { get; set; }
    public string? RemarkOfMer { get; set; }
    public string? RemarkOfPur { get; set; }
    public string? CustomerNote { get; set; }
    public bool? FabricLabdipActivated { get; set; }
    public bool? SentAgain { get; set; }
    public int? OriginalId { get; set; }
    public int StyleFabricRandomId { get; set; }
    public int? RandomId { get; set; }

    [NotMapped]
    public string? StateName => State.GetDisplayName();

    [NotMapped]
    public string? CustomerStatusName => CustomerStatus.GetDisplayName();

    [ForeignKey(nameof(StyleId))]
    public Style? Style { get; set; }

    [ForeignKey(nameof(PurchaseOrderId))]
    public PurchaseOrder? PurchaseOrder { get; set; }

    [ForeignKey(nameof(StyleFabricId))]
    public PoStyleFabric? StyleFabric { get; set; }

    [ForeignKey(nameof(SentToPurByUserId))]
    public AppUser? SentToPurByUser { get; set; }

    [ForeignKey(nameof(SentToMerByUserId))]
    public AppUser? SentToMerByUser { get; set; }

    [ForeignKey(nameof(ReceivedByUserId))]
    public AppUser? ReceivedByUser { get; set; }

    [ForeignKey(nameof(CustomerApprovedByUserId))]
    public AppUser? CustomerApprovedByUser { get; set; }

    [ForeignKey(nameof(CustomerRejectedByUserId))]
    public AppUser? CustomerRejectedByUser { get; set; }

    [ForeignKey(nameof(CustomerCanceledByUserId))]
    public AppUser? CustomerCanceledByUser { get; set; }

    [ForeignKey(nameof(CreatorId))]
    public AppUser? CreatedByUser { get; set; }

    [ForeignKey(nameof(ModifierId))]
    public AppUser? UpdatedByUser { get; set; }
}

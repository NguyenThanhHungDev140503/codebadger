using Domain.Common;
using Domain.Entities.Identity;
using Domain.Entities.Orders;
using Domain.Enums;
using Domain.Shared.Extensions;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Trims;

[Table("ERP.PurchaseOrders.Styles.Trims.LabDips")]
public class StyleTrimLabDip : BaseOrgAuditEntityVersion<int>
{
    [Column("StyleTrimLabDipId")]
    public override int Id { get; set; }

    public string? RequestCode { get; set; }
    public int StyleId { get; set; }
    public int StyleTrimId { get; set; }
    public int PurchaseOrderId { get; set; }
    public int TrimSupplierId { get; set; }

    public DateTime? SentPurDate { get; set; }
    public int? SentToPurByUserId { get; set; }
    public DateTime? ExpectReceiveDate { get; set; }
    public DateTime? ReceiveDateOfPur { get; set; }
    public int? ReceivedByUserId { get; set; }
    public StyleDevelopmentState PurState { get; set; }

    public DateTime? SentToQADate { get; set; }
    public DateTime? ReceiveDateOfQA { get; set; }
    public int? ReceivedByUserIdOfQA { get; set; }
    public LabDipQAState? State { get; set; }
    public byte? QAState { get; set; }

    public DateTime? SentMerDate { get; set; }
    public int? PurchRejectedByUserId { get; set; }
    public DateTime? PurchRejectedDate { get; set; }

    public DateTime? SentCustomerDate { get; set; }
    public DateTime? CustomerApprovedDate { get; set; }
    public int? CustomerApprovedByUserId { get; set; }
    public DateTime? CustomerRejectedDate { get; set; }
    public int? CustomerRejectedByUserId { get; set; }
    public DateTime? CustomerCanceledDate { get; set; }
    public int? CustomerCanceledByUserId { get; set; }
    public StyleDevelopmentCustomerStatus CustomerStatus { get; set; }

    public string? ColorExt { get; set; }
    public string? PantoneExt { get; set; }
    public string? RemarkOfMer { get; set; }
    public string? RemarkOfPur { get; set; }
    public string? RemarkOfQa { get; set; }
    public string? Remark { get; set; }
    public string? CustomerNote { get; set; }

    [NotMapped]
    public string? StateName => State.HasValue ? State.Value.GetDisplayName() : string.Empty;

    [NotMapped]
    public string? PurStateName => PurState.GetDisplayName();

    [NotMapped]
    public string? CustomerStatusName => CustomerStatus.GetDisplayName();

    [ForeignKey(nameof(StyleTrimId))]
    public StyleTrim? StyleTrim { get; set; }

    [ForeignKey(nameof(StyleId))]
    public Style? Style { get; set; }

    [ForeignKey(nameof(PurchaseOrderId))]
    public PurchaseOrder? PurchaseOrder { get; set; }

    [ForeignKey(nameof(TrimSupplierId))]
    public TrimSupplier? TrimSupplier { get; set; }

    [ForeignKey(nameof(SentToPurByUserId))]
    public AppUser? SentToPurByUser { get; set; }

    [ForeignKey(nameof(ReceivedByUserId))]
    public AppUser? ReceivedByUser { get; set; }

    [ForeignKey(nameof(ReceivedByUserIdOfQA))]
    public AppUser? ReceivedByUserOfQA { get; set; }

    [ForeignKey(nameof(PurchRejectedByUserId))]
    public AppUser? PurchRejectedByUser { get; set; }

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

using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Orders;

[Table("ERP.CuttingDocket")]
public class CuttingDocket : BaseOrgAuditEntityVersion<int>
{
    [Column("CuttingDocketId")]
    public override int Id { get; set; }

    public string? Code { get; set; }
    public int? PartnerId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public int? SeasonId { get; set; }
    public int? DropId { get; set; }
    public int? StyleId { get; set; }
    public int? ColorId { get; set; }
    public int? StyleCategoryId { get; set; }
    public DateTime? SentTechnicalDate { get; set; }
    public int? SentTechnicalBy { get; set; }
    public DateTime? ExpectedReceiveDate { get; set; }
    public DateTime? TechnicalReceivedDate { get; set; }
    public int? TechnicalReceivedBy { get; set; }
    public byte? State { get; set; }
    public string? Remark { get; set; }
    public int? Stt { get; set; }
    public DateTime? SentMerDate { get; set; }
    public int? SentMerBy { get; set; }
    public string? RejectedReason { get; set; }
    public int? StyleTechnicalCmpId { get; set; }
    public decimal? Total { get; set; }
    public decimal? TotalExtra { get; set; }
    public int? StyleTechnicalPomId { get; set; }
    public string? RequestCode { get; set; }
    public bool? SentAgain { get; set; }
    public int? OriginalId { get; set; }
    public string? ContactTo { get; set; }
    public int? PersonInChargeUserId { get; set; }
    public int? ProcessTypeId { get; set; }
    public decimal? TotalQuantity { get; set; }
    public string? StyleName { get; set; }

    // CMP (Part G - Sewing Operation Process) của docket. Join: StyleTechnicalCmp.Id = CuttingDocket.StyleTechnicalCmpId.
    [ForeignKey(nameof(StyleTechnicalCmpId))]
    public StyleTechnicalCmp? StyleTechnicalCmp { get; set; }
}

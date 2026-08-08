using Domain.Common;
using Domain.Entities.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Orders;

[Table("ERP.PurchaseOrders.Styles.Technicals.CMP")]
public class StyleTechnicalCmp : BaseOrgAuditEntityVersion<int>, IOperationCost, IConsumption
{
    [Column("StyleTechnicalCmpId")]
    public override int Id { get; set; }

    public string? RequestCode { get; set; }
    public int? StyleId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public DateTime? SentTechnicalDate { get; set; }
    public DateTime? ExpectReceiveDate { get; set; }
    public int? SentToTechnicalByUserId { get; set; }
    public byte? State { get; set; }
    public DateTime? SentMerDate { get; set; }
    public int? SentToMerByUserId { get; set; }
    public bool? SentAgain { get; set; }
    public DateTime? TechnicalReceivedDate { get; set; }
    public int? TechnicalReceivedByUserId { get; set; }
    public string? Remark { get; set; }
    public decimal? TotalCMP { get; set; }
    public decimal? TotalCutting { get; set; }
    public decimal? TotalNumbering { get; set; }
    public decimal? TotalPrepare { get; set; }
    public decimal? TotalSewing { get; set; }
    public decimal? TotalEndLine { get; set; }
    public decimal? TotalPressing { get; set; }
    public decimal? TotalFinishing { get; set; }
    public decimal? TotalPolyBag { get; set; }
    public int? TotalOperation { get; set; }
    public decimal? TotalOperator { get; set; }
    public decimal? TotalRatingOfProductivity { get; set; }
    public bool? SentToBOM { get; set; }
    public string? TechnicalReason { get; set; }
    public DateTime? TechnicalReasonDate { get; set; }
    public int? OriginalId { get; set; }
    public decimal? TotalBartack { get; set; }
    public decimal? TotalCMPConsumption { get; set; }
    public decimal? TotalCuttingConsumption { get; set; }
    public decimal? TotalNumberingConsumption { get; set; }
    public decimal? TotalPrepareConsumption { get; set; }
    public decimal? TotalSewingConsumption { get; set; }
    public decimal? TotalEndLineConsumption { get; set; }
    public decimal? TotalPressingConsumption { get; set; }
    public decimal? TotalFinishingConsumption { get; set; }
    public decimal? TotalPolyBagConsumption { get; set; }
    public decimal? TotalBartackConsumption { get; set; }
    public string? RemarkOfTechnical { get; set; }
    public bool? Deactived { get; set; }
    public byte? CmpType { get; set; }
    
    public ICollection<StyleTechnicalCmpDetail>? Details { get; set; }
}

using Domain.Common;
using Domain.Entities.Configs;
using Domain.Entities.Identity;
using Domain.Entities.Orders;
using Domain.Shared.Extensions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Entities.Interfaces;
using Domain.Enums;

namespace Domain.Entities.Costing;

public abstract class BaseCosting<T> : BaseOrgAuditEntityVersion<T>, ITotalCosting, ICostingWorkflow
    where T : IEquatable<T>
{
    // Common
    public string? Code { get; set; }
    public required string Name { get; set; }
    public DateTime? CostingDate { get; set; }
    public CostingTypes? CostingType { get; set; }
    public CostingStatus? Status { get; set; }
    public CostingCustomerStatus? CustomerStatus { get; set; }

    // ITotalCosting
    public decimal? TotalOfCMP { get; set; }
    public decimal? TotalOfFabric { get; set; }
    public decimal? TotalOfTrim { get; set; }
    public decimal? TotalOfTreatmentInhouse { get; set; }
    public decimal? TotalOfTreatmentOutsource { get; set; }
    public decimal? TotalTesting { get; set; }
    public decimal? TotalGA { get; set; }
    public decimal? TotalMargin { get; set; }
    public decimal? TotalOfEMB { get; set; }
    public decimal? TotalOfAOP { get; set; }
    public decimal? TotalOfDyeWash { get; set; }
    public decimal? TotalPreMargin { get; set; }
    public decimal? TotalAfterMargin { get; set; }
    public decimal? TotalPriceFactoryCMP { get; set; }
    public decimal? TotalPriceFactorySGA { get; set; }
    public decimal? TotalQtyOfSku { get; set; }

    // ICostingWorkflow — Merchandiser
    public DateTime? MerSentDate { get; set; }
    public int? MerSentById { get; set; }

    // ICostingWorkflow — Client
    public DateTime? ClientSentDate { get; set; }
    public int? ClientSentById { get; set; }
    public DateTime? ClientAcceptedDate { get; set; }
    public int? ClientAcceptedById { get; set; }
    public DateTime? ClientRejectedDate { get; set; }
    public int? ClientRejectedById { get; set; }
    public string? ClientRejectReason { get; set; }

    // ICostingWorkflow — Finance
    public DateTime? FinanceApprovedDate { get; set; }
    public int? FinanceApprovedById { get; set; }
    public DateTime? FinanceRejectedDate { get; set; }
    public int? FinanceRejectedById { get; set; }
    public string? FinanceNote { get; set; }

    // ICostingWorkflow — Customer
    public DateTime? CustomerApprovedDate { get; set; }
    public DateTime? CustomerCanceledDate { get; set; }
    public string? CustomerNote { get; set; }
}

[Table("ERP.Costing.Sheets")]
public class CostingSheet : BaseCosting<int>
{
    [Column("CostingSheetId")]
    public override int Id { get; set; }
    public int? PurchaseOrderId { get; set; }
    public int? StyleId { get; set; }
    public decimal? MarginPercent { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public int? RevisionId { get; set; }
    public decimal? ExchangeRateUSDToVND { get; set; }
    public double? CmpFactor { get; set; }
    public double? SgaFactor { get; set; }
    public decimal? TestingPercent { get; set; }
    public int? ExchangeRateId { get; set; }
    public bool? ClientCanView { get; set; }
    public bool? FinanceCanView { get; set; }
    public bool? ByStandardQty { get; set; }
    public decimal? StandardQty { get; set; }
    public int? AllPartPositionId { get; set; }
    public int? CurrencyId { get; set; }
    public int? OriginalId { get; set; }
    public string? Remark { get; set; }
    public decimal? Commission { get; set; }
    public decimal? FOB { get; set; }
    public decimal? FobExpect { get; set; }
    public decimal? PreviousFOB { get; set; }
    public decimal? PreviousMargin { get; set; }
    public string? FabricMOC { get; set; }
    public string? FabricMOQ { get; set; }
    public string? FabricDetails { get; set; }
    public int? TreatmentOnId { get; set; }
    public int? CostingConfigurationId { get; set; }
    public int? StyleColorId { get; set; }
    public int? CostingIdReuse { get; set; }
    public DateTime? SwatchPrintedDate { get; set; }
    public DateTime? SwatchPrintExpireDate { get; set; }
    public string? PathQRCode { get; set; }

    [NotMapped]
    public string? StatusName => Status is null or CostingStatus.Unknown ? null : Status.GetDisplayName();

    [NotMapped]
    public string? CustomerStatusName => CustomerStatus is null or CostingCustomerStatus.None ? null : CustomerStatus.GetDisplayName();

    [NotMapped]
    public string? SwatchStateName
        => (SwatchPrintExpireDate == null || SwatchPrintExpireDate.Value.Date <= DateTime.Today)
            ? SwatchStatus.Renew.GetDisplayName()
            : SwatchStatus.Printed.GetDisplayName();

    [NotMapped]
    public int SwatchState
        => (SwatchPrintExpireDate == null || SwatchPrintExpireDate.Value.Date <= DateTime.Today)
            ? (int)SwatchStatus.Renew
            : (int)SwatchStatus.Printed;

    [ForeignKey(nameof(PurchaseOrderId))]
    public PurchaseOrder? PurchaseOrder { get; set; }

    [ForeignKey(nameof(StyleId))]
    public Style? Style { get; set; }

    [ForeignKey(nameof(MerSentById))]
    public AppUser? MerSentByUser { get; set; }

    [ForeignKey(nameof(ClientSentById))]
    public AppUser? ClientSentByUser { get; set; }

    [ForeignKey(nameof(FinanceApprovedById))]
    public AppUser? FinanceApprovedByUser { get; set; }

    [ForeignKey(nameof(StyleColorId))]
    public Color? StyleColor { get; set; }

    [ForeignKey(nameof(CreatorId))]
    public AppUser? CreatedByUser { get; set; }

    [ForeignKey(nameof(ModifierId))]
    public AppUser? UpdatedByUser { get; set; }

}

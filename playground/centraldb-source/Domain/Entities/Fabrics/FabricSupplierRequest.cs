using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Fabrics
{
    public class FabricSupplierRequest : BaseOrgAuditEntity<int>
    {
        public int FabricSupplierId { get; set; }
        public int? FabricId { get; set; }
        public int? PartnerId { get; set; }
        public int? NationalityId { get; set; }
        public decimal? ConvertRate { get; set; }
        public string? ItemCode { get; set; }
        public decimal? CutWidth { get; set; }
        public decimal? MOC { get; set; }
        public decimal? Shrinkage { get; set; }
        public decimal? Surcharge { get; set; }
        public string? PriceRemark { get; set; }
        public int? OtherFeeTypeId { get; set; }
        public DateTime? PriceQuotedDate { get; set; }
        public decimal? OtherFeePrice { get; set; }
        public DateTime? PriceExpiryDate { get; set; }
        public int? SurchargeCurrencyId { get; set; }
        public int? OtherFeeCurrencyId { get; set; }
        public string? Name { get; set; }
        public string? LeadTimeWithGreigeAvailable { get; set; }
        public byte? LeadTimeWithGreigeType { get; set; }
        public string? LeadTimeNoGreige { get; set; }
        public byte? LeadTimeNoGreigeType { get; set; }
        public int? StyleFabricDevelopmentId { get; set; }
        public decimal? LocalFeePrice { get; set; }
        public int? LocalFeeCurrencyId { get; set; }
        public decimal? NaMoldFeePriceme { get; set; }
        public int? MoldFeeCurrencyId { get; set; }
        public decimal? VATFeePrice { get; set; }
        public int? VATFeeCurrencyId { get; set; }
        public decimal? BankFeePrice { get; set; }
        public int? BankFeeCurrencyId { get; set; }
        public string? FabricSupplierCode { get; set; }
        public bool Activated { get; set; }
        public byte? State { get; set; }
        public DateTime? SentToFinanceDate { get; set; }
        public DateTime? FinanceApprovedDate { get; set; }
        public int? FinanceApprovedByUserId { get; set; }
        public DateTime? FinanceRejectedDate { get; set; }
        public int? FinanceRejectedByUserId { get; set; }
        public string? RemarkOfFinance { get; set; }
        public string? FabricSupplierRequestCode { get; set; }
        public string? FinanceRejectedReason { get; set; }
        public string? RemarkOfPur { get; set; }
        public string? ColumnsChange { get; set; }

        [ForeignKey(nameof(FabricId))]
        public FabricItem? FabricItem { get; set; }
    }
}

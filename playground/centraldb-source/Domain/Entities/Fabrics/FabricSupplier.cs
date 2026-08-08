using Domain.Common;
using Domain.Entities.CRM;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Fabrics
{
    public class FabricSupplier : BaseOrgAuditEntity<int>
    {
        public int FabricId { get; set; }
        public int PartnerId { get; set; }
        public int NationalityId { get; set; }
        public string? ItemCode { get; set; }
        public decimal? ConvertRate { get; set; }
        public decimal? CutWidth { get; set; }
        public decimal? MOC { get; set; }
        public decimal? Shrinkage { get; set; }
        public decimal? Surchage { get; set; }
        public string? PriceRemark { get; set; }
        public int? OtherFeeTypeId { get; set; }
        public DateTime? PriceQuotedDate { get; set; }
        public DateTime? PriceExpiryDate { get; set; }
        public decimal? OtherFeePrice { get; set; }
        public byte? LeadTimeWithGreigeType { get; set; }
        public byte? LeadTimeNoGreigeType { get; set; }
        public decimal? MoldFeePrice { get; set; }
        public decimal? LocalFeePrice { get; set; }
        public decimal? VATFeePrice { get; set; }
        public decimal? BankFeePrice { get; set; }
        public int SurchargeCurrencyId { get; set; }
        public int OtherFeeCurrencyId { get; set; }
        public int LeadTimeWithGreigeAvailable { get; set; }
        public int LeadTimeNoGreige { get; set; }
        public int StyleFabricDevelopmentId { get; set; }
        public int LocalFeeCurrencyId { get; set; }
        public int MoldFeeCurrencyId { get; set; }
        public int VATFeeCurrencyId { get; set; }
        public int BankFeeCurrencyId { get; set; }
        public int ProcessingRequestId { get; set; }
        public int LastApprovedRequestId { get; set; }
        public string? Name { get; set; }
        public string? ProcessingRequestCode { get; set; }
        public string? LastApprovedRequestCode { get; set; }
        public string? RemarkOfPur { get; set; }
        public string? FabricSupplierCode { get; set; }
        public bool? Activated { get; set; }
        public bool? IsDraft { get; set; }

        [ForeignKey(nameof(LastApprovedRequestId))]
        public FabricSupplierRequest? LastApprovedRequest { get; set; }

        [ForeignKey(nameof(PartnerId))]
        public Partner? Partner { get; set; }
    }
}

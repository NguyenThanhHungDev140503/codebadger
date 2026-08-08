using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.CRM
{
    public class PartnersSaleAgent : BaseOrgAuditEntityVersion<int>
    {
        public int PartnerId { get; set; }
        public int ContactId { get; set; }
        public string? Company { get; set; }
        public decimal? Commission { get; set; }
        public string? ContactName { get; set; }
        public string? ContactPosition { get; set; }
        public int SaleAgentTermId { get; set; }

        public SaleAgentTerm? SaleAgentTerm { get; set; }
        [ForeignKey("ContactId")]
        public PartnerContact? Contact { get; set; }
    }
}

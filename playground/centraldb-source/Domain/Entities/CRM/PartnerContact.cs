using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Entities.Configs;

namespace Domain.Entities.CRM
{
    public class PartnerContact : BaseOrgAuditEntityVersion<int>
    {
        public int PartnerId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phones { get; set; }
        public string? Location { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? FinishDate { get; set; }
        public string? ContactName { get; set; }
        public string? ContactPosition { get; set; }
        public int? ContactInChargeId { get; set; }
        public string? Address { get; set; }
        public string? ContactCode { get; set; }
        public int? NationalityId { get; set; }

        [ForeignKey("ContactInChargeId")]
        public ContactInCharge? ContactInCharge { get; set; }

        public ICollection<PartnersSaleAgent>? PartnersSaleAgent { get; set; }   
        
        [ForeignKey("NationalityId")]
        public Nationality? Nationality { get; set; }
    }
}
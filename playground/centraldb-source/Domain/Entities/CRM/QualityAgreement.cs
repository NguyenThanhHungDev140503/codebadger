using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.CRM
{
    public class QualityAgreement : BaseOrgAuditEntityVersion<int>
    {
        [Column("QualityAgreementCode")] 
        public string? Code { get; set; }

        [Column("QualityAgreementName")] 
        public string? Name { get; set; }

        public bool Activated { get; set; }
    }
}

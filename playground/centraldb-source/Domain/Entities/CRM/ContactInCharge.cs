using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.CRM
{
    public class ContactInCharge : BaseOrgAuditEntityVersion<int>
    {
        [Column("ContactInChargeCode")] public string? Code { get; set; }
        [Column("ContactInChargeName")]public string? Name { get; set; }

        public bool? Activated { get; set; }
    }
}

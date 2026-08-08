using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.CRM
{
    public class EnterpriseSize : BaseOrgAuditEntityVersion<int>
    {
        [Column("EnterPriseSizeCode")] public string? Code { get; set; }
        [Column("EnterPriseSizeName")] public string? Name { get; set; }
        public bool Activated { get; set; }
    }
}

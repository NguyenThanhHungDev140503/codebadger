using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.CRM
{
    public class SaleAgentTerm : BaseOrgAuditEntityVersion<int>
    {
        [Column("SaleAgentTermCode")]public string? Code { get; set; }
        [Column("SaleAgentTermName")] public string? Name { get; set; }
        public bool? Activated { get; set; }
    }
}

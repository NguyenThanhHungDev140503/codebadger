using Domain.Common;

namespace Domain.Entities.CRM
{
    public class Delivery : BaseOrgAuditEntityVersion<int>
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
    }
}

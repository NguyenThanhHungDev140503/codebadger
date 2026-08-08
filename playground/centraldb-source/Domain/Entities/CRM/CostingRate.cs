using Domain.Common;

namespace Domain.Entities.CRM
{
    public class CostingRate : BaseOrgAuditEntityVersion<int>
    {
        public string? Rate { get; set; }
    }
}

using Domain.Common;

namespace Domain.Entities.Warehouse
{
    public class Warehouse : BaseAuditEntity<int>, IOrgEntity
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public bool Activated { get; set; }
        public int CompanyId { get; set; }
    }
}

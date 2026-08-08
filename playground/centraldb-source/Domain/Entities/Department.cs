using Domain.Common;

namespace Domain.Entities
{
    public class Department : BaseEntity<int>, IOrgEntity
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public int CompanyId { get; set; }
        public bool Activated { get; set; }

    }
}

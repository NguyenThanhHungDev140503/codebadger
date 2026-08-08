using Domain.Common;

namespace Domain.Entities
{
    public class CompanyConfig : BaseOrgEntity<int>
    {
        public required string KeyConfig { get; set; }
        public string? Value { get; set; }
    }
}

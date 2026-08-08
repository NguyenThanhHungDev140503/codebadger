using Domain.Common;

namespace Domain.Entities.Accountings
{
    public class AppCurrency : BaseAuditEntity<int>
    {
        public required string Name { get; set; }
        public required string Code { get; set; }
        public string? Description { get; set; }
        public string? Symbol { get; set; }
    }
}

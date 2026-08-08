using Domain.Common;

namespace Domain.Entities
{
    public class Company : BaseEntity<int>
    {
        public required string Name { get; set; }
        public string? Address { get; set; }
        public required string SubDomain { get; set; }
        public int MaxUser { get; set; }
        public int Version { get; set; }
        public DateTime? ExpireDate { get; set; }
        public string? DomainUsed { get; set; }
        public bool Disabled { get; set; }
    }
}

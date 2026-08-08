using System.ComponentModel.DataAnnotations;

namespace Domain.Common
{
    public interface IAuditEntity
    {
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        [StringLength(200)]
        public string? Creator { get; set; }
        [StringLength(200)]
        public string? Modifier { get; set; }
        public int? CreatorId { get; set; }
        public int? ModifierId { get; set; }
    }

    public interface IOrgEntity
    {
        public int CompanyId { get; set; }
    }

    public interface IVersionEntity
    {
        public int Version { get; set; }
    }
}

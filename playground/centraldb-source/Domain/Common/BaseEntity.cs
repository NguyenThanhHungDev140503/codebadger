using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Common
{
    public abstract class BaseEntity<TKey> where TKey : IEquatable<TKey>
    {
        public virtual TKey Id { get; set; } = default!;
    }

    public abstract class BaseOrgEntity<TKey> : BaseEntity<TKey>, IOrgEntity where TKey : IEquatable<TKey>
    {
        public int CompanyId { get; set; }
    }

    public abstract class BaseOrgEntityVersion<TKey> : BaseEntity<TKey>, IOrgEntity, IVersionEntity where TKey : IEquatable<TKey>
    {
        public int CompanyId { get; set; }
        [JsonIgnore]
        public int Version { get; set; }
    }

    public class BaseAuditEntity<TKey> : BaseEntity<TKey>, IAuditEntity where TKey : IEquatable<TKey>
    {
        #region Audit
        public DateTime CreatedDate { get; set; }
        [Column("UpdatedDate")] public DateTime? ModifiedDate { get; set; }

        [JsonIgnore]
        [Column("CreatedByUserId")] public int? CreatorId { get; set; }
        [NotMapped] public string? Creator { get; set; }

        [JsonIgnore]
        [Column("UpdatedByUserId")] public int? ModifierId { get; set; }
        [NotMapped] public string? Modifier { get; set; }
        #endregion
    }

    public class BaseOrgAuditEntity<TKey> : BaseAuditEntity<TKey>, IOrgEntity where TKey : IEquatable<TKey>
    {
        public int CompanyId { get; set; }
    }

    public class BaseOrgAuditEntityVersion<TKey> : BaseOrgAuditEntity<TKey>, IVersionEntity where TKey : IEquatable<TKey>
    {
        [JsonIgnore]
        public int Version { get; set; }
    }
}

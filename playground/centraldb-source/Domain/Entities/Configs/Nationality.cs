
using Domain.Common;
using Domain.Entities.CRM;

namespace Domain.Entities.Configs
{
    public class Nationality : BaseAuditEntity<int>, IVersionEntity
    {
        public int ContinentId { get; set; }
        public string? Code { get; set; }
        public int Version { get; set; }
        
        public virtual ICollection<NationalityLanguage> NationalityLanguage { get; set; } = [];
        public virtual ICollection<Partner> Partners { get; set; } = [];
    }
}

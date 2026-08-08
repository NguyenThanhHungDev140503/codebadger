
using Domain.Common;

namespace Domain.Entities.Configs
{
    public class NationalityLanguage : BaseEntity<int>
    {
        //public int NationalityLanguageId { get; set; }
        public int NationalityId { set; get; }
        public int LanguageId { set; get; }
        public string? NationName { set; get; }

        public virtual Nationality? Nationality { get; set; }
    }
}

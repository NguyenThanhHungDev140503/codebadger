using System.ComponentModel.DataAnnotations;

namespace Domain.Enums
{
    public enum UASubSystem : byte
    {
        [Display(Name = "SUB SYSTEM")] NONE = 0,
        [Display(Name = "WEB APP")] ERP = 1,
        [Display(Name = "MOBILE APP")] MOBILE = 2,
    }
}

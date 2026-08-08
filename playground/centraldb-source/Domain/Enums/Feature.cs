using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;

public enum Feature : byte
{
    [Display(Name = "Reporting")] Reporting = 1,
    [Display(Name = "Central DB Sync")] CentralDbSync = 2
}

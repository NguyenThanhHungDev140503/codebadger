using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;

public enum StyleDevelopmentCustomerStatus : byte
{
    [Display(Name = "N/A")]       None      = 0,
    [Display(Name = "Rejected")]  Rejected  = 1,
    [Display(Name = "Approved")]  Approved  = 2,
    [Display(Name = "Cancelled")] Cancelled = 3
}

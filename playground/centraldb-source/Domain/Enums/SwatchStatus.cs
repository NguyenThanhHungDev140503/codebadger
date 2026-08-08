using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;

public enum SwatchStatus : byte
{
    [Display(Name = "-- Unknown --")] Unknown = 0,
    [Display(Name = "Renew")] Renew = 1,
    [Display(Name = "Printed")] Printed = 2,
}

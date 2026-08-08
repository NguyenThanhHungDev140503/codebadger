using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;

public enum SwatchWorkTypes : byte
{
    [Display(Name = "-- Select a mode --")] NA          = 0,
    [Display(Name = "SWATCH CONTINUE")]     Swatch      = 1,
    [Display(Name = "SWITCH TO DEVELOP")]   Development = 2
}

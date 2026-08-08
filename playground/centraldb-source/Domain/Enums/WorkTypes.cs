using System.ComponentModel.DataAnnotations;
namespace Domain.Enums;

public enum WorkTypes : byte
{
    [Display(Name = "-- Work Type --")] Unknown = 0,

    [Display(Name = "Sample")] Sample = 1,

    [Display(Name = "Bulk")] Bulk = 2,

    [Display(Name = "Development")] Development = 3
}

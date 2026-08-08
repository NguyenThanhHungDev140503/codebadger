using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;

public enum CostingTypes : byte
{
    [Display(Name = "-- Costing Type --")] None = 0,
    [Display(Name = "Draft")] Draft = 1,
    [Display(Name = "Sample")] Sample = 2,
    [Display(Name = "Bulk")] Bulk = 3
}

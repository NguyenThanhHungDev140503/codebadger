using System.ComponentModel.DataAnnotations;

namespace Domain.Enums.Reporting;

public enum ReportRunChangeTypes : byte
{
    [Display(Name = "-- Report Run Change Type --")] Unknown = 0,
    [Display(Name = "Inserted")] Inserted = 5,
    [Display(Name = "Updated")] Updated = 10
}
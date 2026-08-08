using System.ComponentModel.DataAnnotations;

namespace Domain.Enums.Reporting;

public enum ReportDateRangeModes : byte
{
    [Display(Name = "-- Report Date Range Mode --")] Unknown = 0,
    [Display(Name = "Required")] Required = 5,
    [Display(Name = "Optional")] Optional = 10,
    [Display(Name = "Disabled")] Disabled = 15
}
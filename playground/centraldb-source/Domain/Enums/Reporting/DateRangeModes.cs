using System.ComponentModel.DataAnnotations;

namespace Domain.Enums.Reporting;

public enum DateRangeModes : byte
{
    [Display(Name = "-- Schedule Date Range Mode --")] Unknown = 0,
    [Display(Name = "Static")] Static = 5,
    [Display(Name = "Relative")] Relative = 10
}
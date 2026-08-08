using System.ComponentModel.DataAnnotations;

namespace Domain.Enums.Reporting;

public enum RelativeRangeTypes : byte
{
    [Display(Name = "-- Relative Range Type --")] Unknown = 0,
    [Display(Name = "LastNDays")] LastNDays = 5,
    [Display(Name = "ThisWeek")] ThisWeek = 10,
    [Display(Name = "LastWeek")] LastWeek = 15,
    [Display(Name = "ThisMonth")] ThisMonth = 20,
    [Display(Name = "LastMonth")] LastMonth = 25,
    [Display(Name = "MTD")] MTD = 30,
    [Display(Name = "QTD")] QTD = 35,
    [Display(Name = "YTD")] YTD = 40
}
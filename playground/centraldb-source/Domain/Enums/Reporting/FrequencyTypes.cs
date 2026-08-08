using System.ComponentModel.DataAnnotations;

namespace Domain.Enums.Reporting;

public enum FrequencyTypes : byte
{
    [Display(Name = "-- Frequency Type --")] Unknown = 0,
    [Display(Name = "OnDemand")] OnDemand = 5,
    [Display(Name = "Daily")] Daily = 10,
    [Display(Name = "Weekly")] Weekly = 15,
    [Display(Name = "Monthly")] Monthly = 20,
    [Display(Name = "Quarterly")] Quarterly = 25,
    [Display(Name = "Yearly")] Yearly = 30,
    [Display(Name = "Cron")] Cron = 35
}
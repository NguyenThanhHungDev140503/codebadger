using System.ComponentModel.DataAnnotations;

namespace Domain.Enums.Reporting;

public enum TriggerTypes : byte
{
    [Display(Name = "-- Report Run Trigger Type --")] Unknown = 0,
    [Display(Name = "Schedule")] Schedule = 5,
    [Display(Name = "Manual")] Manual = 10,
    [Display(Name = "Retry")] Retry = 15,
    [Display(Name = "On Demand")] OnDemand = 20
}

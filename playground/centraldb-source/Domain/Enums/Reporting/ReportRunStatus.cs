using System.ComponentModel.DataAnnotations;

namespace Domain.Enums.Reporting;

public enum ReportRunStatus : byte
{
    [Display(Name = "-- Report Run Status --")] Unknown = 0,
    [Display(Name = "Queued")] Queued = 5,
    [Display(Name = "Running")] Running = 10,
    [Display(Name = "Success")] Success = 15,
    [Display(Name = "Failed")] Failed = 20,
    [Display(Name = "Cancelled")] Cancelled = 25
}
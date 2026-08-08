using System.ComponentModel.DataAnnotations;

namespace Domain.Enums.Reporting;

public enum ReportNotificationTypes : byte
{
    [Display(Name = "-- Report Notification Type --")] Unknown = 0,
    [Display(Name = "ReportCompleted")] ReportCompleted = 5,
    [Display(Name = "ReportFailed")] ReportFailed = 10
}
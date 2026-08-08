using System.ComponentModel.DataAnnotations;

namespace Domain.Enums.Reporting;

public enum GroupCodes : byte
{
    [Display(Name = "-- Report Group --")] Unknown = 0,
    [Display(Name = "General")] General = 5,
    [Display(Name = "Technical")] Technical = 10,
    [Display(Name = "Purchasing")] Purchasing = 15,
    [Display(Name = "Production")] Production = 20,
    [Display(Name = "QMS")] QMS = 25
}
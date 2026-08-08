using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;

/// <summary>
/// Purchase order state. Bit-flag values mirror CORE-ERP's POStatus.
/// </summary>
public enum POStatus : byte
{
    [Display(Name = "-- Select Status --")] Unknown = 0,
    [Display(Name = "Draft")] Draft = 1,
    [Display(Name = "Waiting Accept")] Waiting = 2,
    [Display(Name = "Accepted")] Approved = 4,
    [Display(Name = "Reject")] Reject = 64
}


public enum MerchStatus : byte
{
    [Display(Name = "-- Merch Status --")] Unknown = 0,
    [Display(Name = "On-going")] Processing = 1,
    [Display(Name = "Developing")] Developing = 5,
    [Display(Name = "Cancel")] Cancel = 10,
    [Display(Name = "Finished")] Finished = 15,
}
 
using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;

public enum StyleDevelopmentState : byte
{
    [Display(Name = "N/A")]      None     = 0,
    [Display(Name = "Waiting")]  Waiting  = 2,
    [Display(Name = "Accepted")] Accepted = 8,
    [Display(Name = "Finished")] Finished = 16,
    [Display(Name = "Rejected")] Rejected = 32
}

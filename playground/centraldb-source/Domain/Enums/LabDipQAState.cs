using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;

public enum LabDipQAState : byte
{
    [Display(Name = "N/A")]
    None = 0,

    [Display(Name = "Waiting")]
    Waiting = 2,

    [Display(Name = "Accepted")]
    Accepted = 8,

    [Display(Name = "Pass")]
    Pass = 16,

    [Display(Name = "Fail")]
    Fail = 32
}

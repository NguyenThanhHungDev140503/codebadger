using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;

public enum FeatureUnavailableReason : byte
{
    [Display(Name = "Config Missing")] ConfigMissing = 0,
    [Display(Name = "Connection Failed")] ConnectionFailed = 1
}

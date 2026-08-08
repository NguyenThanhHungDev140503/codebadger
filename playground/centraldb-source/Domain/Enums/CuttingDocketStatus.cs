using Domain.Shared.Attributes;
using Domain.Shared.Constants;

namespace Domain.Enums;

/// <summary>
/// Trạng thái Cutting Docket. "Finished" (State = Approved) nghĩa là docket đã hoàn tất/duyệt.
/// </summary>
public enum CuttingDocketStatus : byte
{
    [CDStatus(Name = "-- Select CD Status --", BgColor = "bg-black")] Unknown = 0,
    [CDStatus(Name = "Draft", BgColor = Const.GRAY_HEX)] Draft = 1,
    [CDStatus(Name = "Waiting Accept", BgColor = "bg-yellow")] Waiting = 2,
    [CDStatus(Name = "Accepted", BgColor = "bg-blue")] Accepted = 3,
    [CDStatus(Name = "Finished", BgColor = "bg-green")] Approved = 4,
    [CDStatus(Name = "Rejected", BgColor = "bg-red")] Reject = 64,
}

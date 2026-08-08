using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Qms;

/// <summary>
/// Danh mục nhóm lỗi QC — [ERP.QMS.QCDefectGroups].
/// <see cref="ReworkInCharge"/> (GarmentProcess) = bộ phận chịu trách nhiệm rework;
/// nullable vì master data có thể chưa gán (cột tinyint NULL) — row chưa gán sẽ tự bị loại khỏi filter IN(...).
/// </summary>
public class QCDefectGroup : BaseOrgAuditEntityVersion<int>
{
    public string? QCDefectGroupCode { get; set; }
    public string? QCDefectGroupName { get; set; }
    public int? QCStationId { get; set; }
    public bool? Activated { get; set; }
    public GarmentProcess? ReworkInCharge { get; set; }
}

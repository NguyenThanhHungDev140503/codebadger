using Domain.Common;

namespace Domain.Entities.Qms.Endline;

/// <summary>
/// Bảng lỗi QC Endline — [ERP.QMS.QCEndlines.Defects].
/// Bảng chỉ có audit tạo (CreatedDate/CreatedByUserId) + soft-delete (Removed/RemovedDate/RemovedByUserId),
/// không có cột Updated* nên kế thừa <see cref="BaseOrgEntity{TKey}"/> và khai báo audit thủ công.
/// </summary>
public class QCEndlineDefect : BaseOrgEntity<int>
{
    public int QCEndlineId { get; set; }
    public int QCDefectGroupId { get; set; }
    public int QCDefectDetailId { get; set; }
    public int TagId { get; set; }
    public int StyleId { get; set; }
    public int SizeId { get; set; }
    public int ReworkId { get; set; }
    public string? StyleNoInternal { get; set; }
    public bool SSizeRemark { get; set; }

    public Area AreaType { get; set; }
    public QCInspectionType InspectionType { get; set; }

    public DateTime? CreatedDate { get; set; }
    public int CreatedByUserId { get; set; }

    // Soft-delete
    public bool Removed { get; set; }
    public DateTime? RemovedDate { get; set; }
    public int RemovedByUserId { get; set; }
}

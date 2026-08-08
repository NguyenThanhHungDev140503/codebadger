using Domain.Common;
using Domain.Entities.Orders;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Qms.Endline;

/// <summary>
/// Báo cáo QC Endline theo ngày — [ERP.QMS.QCEndlines.DailyReports].
/// Bảng không có cột audit nên kế thừa <see cref="BaseOrgEntity{TKey}"/> (Id + CompanyId).
/// TotalInspection / TotalGarmentInspected là cột computed trong DB (xem Configuration).
/// </summary>
public class QCEndlineReportDaily : BaseOrgEntity<int>
{
    public DateTime? InspectionDate { get; set; }
    public int StyleId { get; set; }
    public QCInspectionType InspectionType { get; set; }
    public int UserIdQC { get; set; }

    public int TotalInspection { get; set; }
    public int TotalGarmentInspected { get; set; }
    public int TotalPass { get; set; }
    public int TotalFail { get; set; }
    public int TotalRework { get; set; }

    public string? LineIds { get; set; }

    [ForeignKey(nameof(StyleId))]
    public Style? Style { get; set; }
}

using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Qms;

/// <summary>
/// Catalog các danh sách lỗi.
/// </summary>
[Table("ERP.QMS.QCDefectDetails")]
public class QCDefectDetail : BaseOrgEntity<int>
{
    [Column("QCDefectDetailId")]
    public override int Id { get; set; }

    public int QCDefectId { get; set; }
    public string? QCDefectCodeInternal { get; set; }
    public string? QCDefectCode { get; set; }
    public string? QCDefectNameVN { get; set; }
    public string? QCDefectNameEN { get; set; }
    public int Stt { get; set; }
    public bool Fail { get; set; }
}

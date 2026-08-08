using Domain.Common;
using Domain.Entities.Configs;
using Domain.Entities.Orders;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Productions;

public class PoStyleSize : BaseEntity<int>
{
    public int? StyleId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public int SizeId { get; set; }

    public int CutQty { get; set; }
    public decimal Quantity { get; set; }
    public decimal Extra { get; set; }
    public decimal? ActualCDQty { get; set; }

    // Nullable như SkuForList: NULL = trạm chưa có số. Việc quy NULL -> 0 chỉ áp cho
    // trạm nằm trong process (xem StyleSizeProcess), trạm ngoài process giữ trống.
    public int? NumberingOut { get; set; }
    public int? PrepareOut { get; set; }
    public int? PrintingPass { get; set; }
    public int? OutsourcePass { get; set; }
    public int? DyewashPass { get; set; }
    public int? EmbPass { get; set; }
    public int? SewingOut { get; set; }
    public int? ThreadSuckingOut { get; set; }
    public int? PressingOut { get; set; }
    public int? FinishingOut { get; set; }
    public int? PackingIn { get; set; }

    /// <summary>
    /// List ProcessItem (col <c>Json</c>). Cho biết trạm nào nằm trong process của size này —
    /// trạm trong process mà chưa có số thì hiển thị 0, trạm ngoài process để trống.
    /// </summary>
    [Column("Json")]
    public string? ProcessJson { get; set; }

    [ForeignKey(nameof(StyleId))]
    public Style? Style { get; set; }

    [ForeignKey(nameof(SizeId))]
    public Size? Size { get; set; }
}

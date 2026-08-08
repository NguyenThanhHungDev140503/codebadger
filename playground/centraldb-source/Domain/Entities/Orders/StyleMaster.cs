namespace Domain.Entities.Orders;

/// <summary>
/// WIP master record per style (table <c>ERP.PurchaseOrders.Styles.Masters</c>).
/// One row per <see cref="StyleId"/> — LEFT-joined to <see cref="Style"/> to expose
/// <see cref="CuttingStartDate"/>. Keyed on StyleId (the join key), matching the ERP
/// pattern: Master (WIP) theo StyleId -> Cutting Start Date.
/// </summary>
public class StyleMaster
{
    public int StyleId { get; set; }

    public DateTime? CuttingStartDate { get; set; }
}

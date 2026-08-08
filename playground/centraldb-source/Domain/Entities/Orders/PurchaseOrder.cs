using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;
using Domain.Entities.CRM;
using Domain.Entities.Configs;
using Domain.Shared.Extensions;
using Domain.Enums;

namespace Domain.Entities.Orders;

public class PurchaseOrder : BaseOrgAuditEntityVersion<int>
{
    #region Properties
    public string? Code { get; set; }
    public int? PersonInChargeUserId { get; set; }
    public DateTime? ReceiveDate { get; set; }
    public byte State { get; set; }
    public int? SupplierId { get; set; }
    public string? Description { get; set; }
    public bool? HasTechpackFile { get; set; }
    public WorkTypes? WorkType { get; set; }
    public int? SeasonId { get; set; }
    [Column("CFC")] public string? OrderExternal { get; set; }
    public decimal? TotalOrderQty { get; set; }

    public MerchStatus? MerStatus { get; set; }

    [NotMapped]
    public string? WorkTypeName => WorkType?.GetDisplayName();
    #endregion

    #region Navigation properties
    public Partner? Supplier { get; set; }
    public Season? Season { get; set; }
    #endregion
}

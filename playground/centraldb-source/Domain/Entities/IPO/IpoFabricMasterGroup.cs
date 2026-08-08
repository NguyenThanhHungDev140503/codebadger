using Domain.Common;
using Domain.Entities.Configs;
using Domain.Entities.CRM;
using Domain.Entities.Fabrics;
using Domain.Shared.Extensions;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.IPO
{
    public class IpoFabricMasterGroup : BaseEntity<int>, IOrgEntity
    {
        #region Properties
        public int IpoFabricMasterId { get; set; }
        public int CompanyId { get; set; }
        /// <summary>
        /// IPOFabricMasterGroupCode
        /// </summary>
        [Column("IPOFabricMasterGroupCode")]
        public string? FabricFastCode { get; set; }
        [Column("SupplierPartnerId")]
        public int? SupplierId { get; set; }
        [Column("CustomerPartnerId")]
        public int? CustomerId { get; set; }
        public int? FabricSupplierId { get; set; }
        public string? LDCode { get; set; }
        public string? LDShadeband { get; set; }
        public string? FabricSupplierCode { get; set; }
        public string? FabricSupplierName { get; set; }
        public string? LocationIds { get; set; }
        public string? LocationNames { get; set; }
        public int? UnitId { get; set; }
        public string? ColorExts { get; set; }
        public string? LDCodeOption { get; set; }
        public decimal? CutWidth { get; set; }
        public bool? IsPaid { get; set; }
        public DateTime? PaymentUpdatedDate { get; set; }
        public DateTime? ConfirmExpectStockInDate { get; set; }

        public string? IpoFabricItemCodeCombined { get; set; }
        public string? ItemCode { get; set; }
        public string? SeasonNames { get; set; }
        public string? ProcessTypeNames { get; set; }
        public string? DropCodes { get; set; }
        public bool? GreigeAvailable { get; set; }
        public string? TreatmentTypes { get; set; }
        public PurchActualShipMode? PurchActualShipMode { get; set; }
        [NotMapped]
        public string? ActualShipModeName => PurchActualShipMode.GetDisplayName();
        public string? GroupFabricTypes { get; set; }
        public decimal? AvgUnitPrice { get; set; }
        public decimal? TotalOrderQty { get; set; }
        public decimal? Inventory { get; set; }
        public decimal? ActualOrderQty { get; set; }
        public double? ActualUnitPrice { get; set; }
        public decimal? Surcharge { get; set; }
        public DateTime? EtaFabricReceivedDate { get; set; }
        public decimal? ActualAmount { get; set; }
        public double? RawActualAmount { get; set; }
        public decimal? PreArrival { get; set; }
        public decimal? TolerancePlus { get; set; }
        public decimal? ToleranceMinus { get; set; }
        public string? PurRemark { get; set; }
        public string? MerRemark { get; set; }
        public string? CommercialRemark { get; set; }
        public DateTime? MinExpectStockInDate { get; set; }
        public DateTime? ShipDateMin { get; set; }
        public DateTime? RevisedStockInDate { get; set; }
        public DateTime? StockInDate { get; set; }
        public bool? UnderMOQ { get; set; }
        public DateTime? ActShippingSampleReceivedDate { get; set; }
        public int? LOTReceive { get; set; }
        public decimal? QtyReceive { get; set; }
        #endregion

        #region Navigation Properties
        [ForeignKey(nameof(IpoFabricMasterId))]
        public IpoFabricMaster? POFabricMaster { get; set; }

        [ForeignKey(nameof(UnitId))]
        public Unit? Unit { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public Partner? Supplier { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Partner? Customer { get; set; }

        [ForeignKey(nameof(FabricSupplierId))]
        public FabricSupplier? FabricSupplier { get; set; }

        public ICollection<IpoFabricMasterGroupDetail> Details { get; set; } = [];

        #endregion
    }
}

using System.ComponentModel.DataAnnotations;

namespace Domain.Enums
{
    public enum RollStates : byte
    {
        [Display(Name = "-- Roll Status --")]
        Unknown        = 0,
        [Display(Name = "Waiting Request")]
        MarkAsReturnSupplier = 1,
        [Display(Name = "Waiting Stock-in")]
        WaitingStockin = 2,
        [Display(Name = "Stock-in")]
        StockIn = 3,
        [Display(Name = "Waiting Warehouse Confirm")]
        WaitingWHConfirmReturn = 4,
        [Display(Name = "Returned")]
        ReturnedSupplier = 5,
        [Display(Name = "Warehouse Rejected")]
        RejectedReturn = 6,
        /// <summary>
        /// Bị remove ở lưới detail trong request return => về trạng thái này (ý nói sẽ không return ở lần request đó mà đợi sang request mới)
        /// </summary>
        [Display(Name = "Waiting New Request")]
        WaitingReturn = 7,
    }
}

using System.ComponentModel.DataAnnotations;

namespace Domain.Enums
{
    public enum StockInSearchBy : byte
    {
        Roll = 1,
        Pallet = 2,
    }

    public enum ComboTypes
    {
        PO = 1,
        LOT = 2,
    }

    public enum PalletFilterOptions
    {
        [Display(Name = "-- Filter Option --")]
        NA = 0,
        [Display(Name = "Empty Pallets")]
        ByEmptyPallet = 1,
        [Display(Name = "No Empty Pallets")]
        NoEmptyPallet = 2,
        [Display(Name = "No Location")]
        NoLocation = 3,
        [Display(Name = "Ready To Use")]
        ReadyToUse = 4
    }
}

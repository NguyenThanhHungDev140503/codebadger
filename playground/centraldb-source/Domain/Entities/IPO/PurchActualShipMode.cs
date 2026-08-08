using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.IPO
{
    public enum PurchActualShipMode : byte
    {
        [Display(Name = "")] Unknown = 0,
        [Display(Name = "SEA")] SEA = 1,
        [Display(Name = "AIR")] AIR = 2,
        [Display(Name = "DHL")] DHL = 3,
        [Display(Name = "FEDEX")] FEDEX = 4,
        [Display(Name = "TRUCK")] TRUCK = 5,
        [Display(Name = "UPS")] UPS = 6,
        [Display(Name = "GRAB")] GRAB = 7,
        [Display(Name = "VIETTEL")] VIETTEL = 8,
        [Display(Name = "LOCAL")] LOCAL = 10,
    }
}
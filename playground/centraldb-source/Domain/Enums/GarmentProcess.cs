using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;

/// <summary>
/// Trạm sản xuất garment. Giá trị mirror CORE-ERP <c>GarmentProcess</c> — dùng để đọc
/// cột <c>Json</c> của StyleSize (biết trạm nào nằm trong process của style/size).
/// </summary>
public enum GarmentProcess : byte
{
    [Display(Name = "-- Station --")] Unknown = 0,
    [Display(Name = "Cutting")] Cutting = 1,
    [Display(Name = "Numbering")] Numbering = 2,
    [Display(Name = "Preparing")] Prepare = 3,
    [Display(Name = "Print In-house")] PrintInhouse = 4,
    [Display(Name = "Print Out-source")] PrintOutsource = 5,
    [Display(Name = "Embellishment")] Embellishment = 6,
    [Display(Name = "Dyewash")] Dyewash = 7,
    [Display(Name = "Sewing")] Sewing = 8,
    [Display(Name = "Pressing")] Pressing = 9,
    [Display(Name = "Finishing")] Finishing = 10,
    [Display(Name = "Packing")] Packing = 11,
    [Display(Name = "ThreadSucking")] ThreadSucking = 12,
    [Display(Name = "Tracking Location")] TrackingLocation = 13
}

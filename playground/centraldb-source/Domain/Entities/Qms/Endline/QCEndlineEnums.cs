using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Qms.Endline;

/// <summary>
/// Loại nghiệm thu QC (cột InspectionType) — tinyint.
/// </summary>
public enum QCInspectionType : byte
{
    [Display(Name = "-- Inspection Type --")] Unknown = 0,
    [Display(Name = "QC Endline")] QCEndline = 1,
    [Display(Name = "Before Dyewash")] BeforeDyewash = 2,
    [Display(Name = "After Dyewash")] AfterDyewash = 3,
    [Display(Name = "QC Final")] QCFinal = 4,
}

/// <summary>
/// Vùng lỗi trên sản phẩm (cột AreaType) — tinyint.
/// </summary>
public enum Area : byte
{
    [Display(Name = "-- Area --")] Unknown = 0,
    [Display(Name = "Inside")] Inside = 1,
    [Display(Name = "Outside")] Outside = 2,
}

using System.ComponentModel.DataAnnotations;

namespace Domain.Enums.Reporting;

public enum FileFormats : byte
{
    [Display(Name = "-- File Formats --")] Unknown = 0,
    [Display(Name = "Excel")] Excel = 5,
    [Display(Name = "CSV")] CSV = 10,
    [Display(Name = "PDF")] PDF = 15
}
using System.ComponentModel.DataAnnotations;

namespace Domain.Enums.Reporting;

public enum DataTypes : byte
{
    [Display(Name = "-- Data Type --")] Unknown = 0,
    [Display(Name = "String")] String = 5,
    [Display(Name = "Int")] Int = 10,
    [Display(Name = "Long")] Long = 15,
    [Display(Name = "Decimal")] Decimal = 20,
    [Display(Name = "Bool")] Bool = 25,
    [Display(Name = "Date")] Date = 30,
    [Display(Name = "IntList")] IntList = 35,
    [Display(Name = "StringList")] StringList = 40
}
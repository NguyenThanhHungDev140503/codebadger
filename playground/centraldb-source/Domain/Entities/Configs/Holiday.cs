using Domain.Common;

namespace Domain.Entities.Configs;

public class Holiday : BaseOrgAuditEntityVersion<int>
{
    public string? Name { get; set; }
    public int? Month { get; set; }
    public int? Day { get; set; }
    public int? Year { get; set; }
    public byte? CalendarType { get; set; }
    public byte? HolidayRepeatType { get; set; }
    public DateTime? Date { get; set; }
    public string? BackgroundColor { get; set; }
    public string? BorderColor { get; set; }
}

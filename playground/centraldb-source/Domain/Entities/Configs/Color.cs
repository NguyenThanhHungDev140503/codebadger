using Domain.Common;

namespace Domain.Entities.Configs;

public class Color : BaseEntity<int>, IOrgEntity
{
    public int CompanyId { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? ColorRGB { get; set; }
    public string? NameCode { get; set; }
}
using Domain.Common;

namespace Domain.Entities.Configs
{
    public class Unit : BaseEntity<int>, IOrgEntity
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
        public decimal? DefaultValue { get; set; }
        public byte? UnitType { get; set; }
        public byte? NumberType { get; set; }
        public int CompanyId { get; set; }
    }
}

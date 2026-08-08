namespace Domain.Entities.Interfaces;

public interface IConsumption
{
    decimal? TotalCMPConsumption { get; set; }
    decimal? TotalCuttingConsumption { get; set; }
    decimal? TotalNumberingConsumption { get; set; }
    decimal? TotalPrepareConsumption { get; set; }
    decimal? TotalSewingConsumption { get; set; }
    decimal? TotalEndLineConsumption { get; set; }
    decimal? TotalPressingConsumption { get; set; }
    decimal? TotalFinishingConsumption { get; set; }
    decimal? TotalPolyBagConsumption { get; set; }
    decimal? TotalBartackConsumption { get; set; }
}

namespace Domain.Entities.Interfaces;

public interface ICostingPrice
{
    decimal? Commission { get; set; }
    decimal? FOB { get; set; }
    decimal? FobExpect { get; set; }
    decimal? PreviousFOB { get; set; }
    decimal? PreviousMargin { get; set; }
}

namespace Domain.Entities.Interfaces;

public interface ITotalCosting
{
    decimal? TotalOfCMP { get; set; }
    decimal? TotalOfFabric { get; set; }
    decimal? TotalOfTrim { get; set; }
    decimal? TotalOfTreatmentInhouse { get; set; }
    decimal? TotalOfTreatmentOutsource { get; set; }
    decimal? TotalTesting { get; set; }
    decimal? TotalGA { get; set; }
    decimal? TotalMargin { get; set; }
    decimal? TotalOfEMB { get; set; }
    decimal? TotalOfAOP { get; set; }
    decimal? TotalOfDyeWash { get; set; }
    decimal? TotalPreMargin { get; set; }
    decimal? TotalAfterMargin { get; set; }
    decimal? TotalPriceFactoryCMP { get; set; }
    decimal? TotalPriceFactorySGA { get; set; }
    decimal? TotalQtyOfSku { get; set; }
}

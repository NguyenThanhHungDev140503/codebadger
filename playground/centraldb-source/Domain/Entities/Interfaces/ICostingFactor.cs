namespace Domain.Entities.Interfaces;

public interface ICostingFactor
{
    double? CmpFactor { get; set; }
    double? SgaFactor { get; set; }
}

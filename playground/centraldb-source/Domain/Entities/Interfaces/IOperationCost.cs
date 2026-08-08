namespace Domain.Entities.Interfaces;

public interface IOperationCost
{
    decimal? TotalCMP { get; set; }
    decimal? TotalCutting { get; set; }
    decimal? TotalNumbering { get; set; }
    decimal? TotalPrepare { get; set; }
    decimal? TotalSewing { get; set; }
    decimal? TotalEndLine { get; set; }
    decimal? TotalPressing { get; set; }
    decimal? TotalFinishing { get; set; }
    decimal? TotalPolyBag { get; set; }
    decimal? TotalBartack { get; set; }
    int? TotalOperation { get; set; }
    decimal? TotalOperator { get; set; }
    decimal? TotalRatingOfProductivity { get; set; }
}

using Application.Abstractions.Messaging;
using FluentValidation;

namespace Application.Common.Models;

/// <summary>
/// Request kết hợp lọc theo khoảng ngày và phân trang.
/// </summary>
public class DateRangePaginationRequest<TResponse>
    : IQuery<TResponse>, IDateRangeRequest
    where TResponse : class
{
    /// <summary>Inclusive start date. Send as <c>yyyy-MM-dd</c>, for example <c>2026-07-01</c>.</summary>
    public DateTime FromDate { get; set; }
    /// <summary>Inclusive end date. Send as <c>yyyy-MM-dd</c>, for example <c>2026-07-31</c>.</summary>
    public DateTime ToDate { get; set; }

    private int _pageIndex = 1;
    private int _pageSize = 10;

    /// <summary>1-based page number. Defaults to 1.</summary>
    public int PageIndex
    {
        get => _pageIndex;
        set => _pageIndex = value <= 0 ? 1 : value;
    }

    /// <summary>Number of records per page. API validation accepts 1-400.</summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value <= 0 ? 1 : Math.Min(value, PaginationDefaults.HardCap);
    }
}

/// <summary>
/// Validator dùng chung cho request có khoảng ngày và phân trang.
/// </summary>
public class DateRangePaginationRequestValidator<T, TResponse>
    : AbstractValidator<T>
    where T : DateRangePaginationRequest<TResponse>
    where TResponse : class
{
    public DateRangePaginationRequestValidator()
    {
        RuleFor(x => x.FromDate)
            .NotEmpty()
            .WithMessage("FromDate is required.");

        RuleFor(x => x.ToDate)
            .NotEmpty()
            .WithMessage("ToDate is required.");

        RuleFor(x => x)
            .Must(x => x.FromDate <= x.ToDate)
            .WithMessage("FromDate must be earlier than or equal to ToDate.");

        RuleFor(x => x.PageIndex)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(PaginationDefaults.MaxPageSize)
            .WithMessage($"PageSize must be between 1 and {PaginationDefaults.MaxPageSize}.");
    }
}

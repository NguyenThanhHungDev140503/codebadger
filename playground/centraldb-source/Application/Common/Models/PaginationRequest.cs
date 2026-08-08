using Application.Abstractions.Messaging;
using Domain.Shared.Constants;
using FluentValidation;

namespace Application.Common.Models
{
    public interface IPaginationRequest
    {
        string? SearchTerm { get; set; }
        int PageIndex { get; set; }
        int PageSize { get; set; }
    }

    public class PaginationRequest<TResponse> : IQuery<TResponse>, IPaginationRequest where TResponse : class
    {
        private int _pageIndex = 1;
        private int _pageSize = 10;
        private string _order = AppConst.SORT_DESC;

        /// <summary>Optional free-text filter. Maximum length is 100 characters.</summary>
        public string? SearchTerm { get; set; }
        /// <summary>Optional field name used for sorting. Allowed values depend on the endpoint.</summary>
        public string? SortField { get; set; }
        /// <summary>Sort direction: <c>asc</c> or <c>desc</c>. Defaults to <c>desc</c>.</summary>
        public string SortOrder { get => _order; set => _order = string.IsNullOrEmpty(value) ? AppConst.SORT_DESC : value; }
        /// <summary>1-based page number. Defaults to 1.</summary>
        public int PageIndex { get => _pageIndex; set => _pageIndex = value <= 0 ? 1 : value; }
        // Lưới an toàn cứng cho request không đi qua validation pipeline.
        // Ngưỡng hợp đồng API (PaginationDefaults.MaxPageSize) do PaginationRequestValidator enforce.
        /// <summary>Number of records per page. API validation accepts 1-400.</summary>
        public int PageSize { get => _pageSize; set => _pageSize = value <= 0 ? 1 : Math.Min(value, PaginationDefaults.HardCap); }

        public PaginationRequest(int page = 1, int size = 10, string orderby = "")
        {
            SortOrder = orderby;
            PageIndex = page;
            PageSize = size;
        }
    }

    public static class PaginationDefaults
    {
        /// <summary>Ngưỡng hợp đồng API — vượt quá sẽ bị validator trả 400.</summary>
        public const int MaxPageSize = 400;

        /// <summary>Lưới an toàn cứng ở setter cho request không đi qua validation pipeline.</summary>
        public const int HardCap = 1000;
    }

    public class PaginationRequestValidator<T> : AbstractValidator<T> where T : IPaginationRequest
    {
        public PaginationRequestValidator()
        {
            RuleFor(x => x.SearchTerm)
            .Must(x => x == null || x.Length <= 100)
            .WithMessage("SearchTerm too long");

            RuleFor(x => x.PageIndex)
                .GreaterThan(0);

            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .LessThanOrEqualTo(PaginationDefaults.MaxPageSize)
                .WithMessage($"PageSize must be between 1 and {PaginationDefaults.MaxPageSize}.");
        }
    }

}

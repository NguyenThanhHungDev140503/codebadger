using Application.Abstractions.Messaging;
using FluentValidation;

namespace Application.Common.Models
{
    /// <summary>
    /// Hợp đồng cho mọi request lọc theo công ty + khoảng ngày.
    /// Bất kỳ query nào cần 3 tham số CompanyId / FromDate / ToDate đều implement interface này
    /// (trực tiếp hoặc qua <see cref="DateRangeRequest{TResponse}"/>).
    /// </summary>
    public interface IDateRangeRequest
    {
        DateTime FromDate { get; set; }
        DateTime ToDate { get; set; }
    }

    /// <summary>
    /// Base request cho các API lọc theo công ty + khoảng ngày.
    /// Kế thừa để có sẵn 3 tham số bắt buộc, đồng thời là <see cref="IQuery{TResponse}"/> cho MediatR.
    /// </summary>
    public class DateRangeRequest<TResponse> : IQuery<TResponse>, IDateRangeRequest where TResponse : class
    {
        /// <summary>Inclusive start date. Send as <c>yyyy-MM-dd</c>, for example <c>2026-07-01</c>.</summary>
        public DateTime FromDate { get; set; }
        /// <summary>Inclusive end date. Send as <c>yyyy-MM-dd</c>, for example <c>2026-07-31</c>.</summary>
        public DateTime ToDate { get; set; }
    }

    /// <summary>
    /// Validator dùng chung: enforce CompanyId hợp lệ và FromDate &lt;= ToDate.
    /// Kế thừa ở validator cụ thể của từng query để thừa hưởng các rule này.
    /// </summary>
    public class DateRangeRequestValidator<T> : AbstractValidator<T> where T : IDateRangeRequest
    {
        public DateRangeRequestValidator()
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
        }
    }
}

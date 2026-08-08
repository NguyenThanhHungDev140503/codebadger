using FluentValidation.Results;

namespace Application.Common.Exceptions
{
    public class BadRequestException : Exception
    {
        public BadRequestException(string message) : base(message)
        {
        }

        public BadRequestException(string message, ValidationResult validationResult) : base(message)
        {
            ValidationErrors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
        }

        public IDictionary<string, string[]>? ValidationErrors { get; set; }
    }
}

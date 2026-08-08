using MediatR;

namespace Application.Common.Behaviours
{
    public class TrimStringBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var stringProps = typeof(TRequest)
                .GetProperties()
                .Where(p => p.PropertyType == typeof(string));

            foreach (var prop in stringProps)
            {
                var value = (string?)prop.GetValue(request);
                if (value != null)
                {
                    prop.SetValue(request, value.Trim());
                }
            }

            return await next();
        }
    }
}

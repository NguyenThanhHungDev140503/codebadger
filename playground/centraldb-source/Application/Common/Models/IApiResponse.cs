namespace Application.Common.Models
{
    public interface IApiResponse<T>
    {
        T? Data { get; set; }
    }
}

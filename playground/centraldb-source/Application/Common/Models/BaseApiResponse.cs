using Microsoft.AspNetCore.Http;

namespace Application.Common.Models
{
    public class BaseApiResponse<T> : IApiResponse<T>
    {
        public bool Successed { get; set; } = true;
        public int ResponseCode { get; set; } = 200;
        public string? Message { get; set; }
        public T? Data { get; set; }
        public IDictionary<string, string[]> Errors { get; set; } = new Dictionary<string, string[]>();
    }

    public class ApiResponse<T> : BaseApiResponse<T>
    {
        public ApiResponse() { }

        public ApiResponse(bool successed = true, int statusCode = 200, T? data = default, string? msg = null, Dictionary<string, string[]>? errors = null)
        {
            Successed = successed;
            ResponseCode = statusCode;
            Message = msg;
            Data = data;
            Errors = errors ?? [];
        }

        public static ApiResponse<T> Success(string? msg = null) => new (msg: msg);
        public static ApiResponse<T> Success(T? data = default) => new(data: data);
        public static ApiResponse<T> Success(T? data = default, string? msg = null) => new(data: data, msg: msg);

        public static ApiResponse<T> Failure(string? msg = null) => new(false, StatusCodes.Status400BadRequest, msg: msg);
        public static ApiResponse<T> Failure(string? msg = null, int statusCode = StatusCodes.Status400BadRequest) => new(false, statusCode, msg: msg);
        public static ApiResponse<T> Failure(T? data = default, string? msg = null, int statusCode = StatusCodes.Status400BadRequest) => new(false, statusCode, data, msg);

        public static ApiResponse<T> Failure(T? data = default, string? msg = null, Dictionary<string, string[]>? errors = null)
            => new(false, StatusCodes.Status400BadRequest, data, msg, errors);
    }
}

namespace Onion.Common.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string? ErrorCode { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public IEnumerable<string> Errors { get; set; } = Array.Empty<string>();

        public static ApiResponse<T> Ok(T data, string? message = null)
            => new ApiResponse<T> { Success = true, StatusCode = 200, Data = data, Message = message, Errors = Array.Empty<string>() };

        public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null, int statusCode = 400, string? errorCode = null)
            => new ApiResponse<T> { Success = false, StatusCode = statusCode, ErrorCode = errorCode ?? errors?.FirstOrDefault(), Message = message, Errors = errors ?? Array.Empty<string>() };
    }
}

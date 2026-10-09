namespace Onion.Common.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public IEnumerable<string> Errors { get; set; } = Array.Empty<string>();

        public static ApiResponse<T> Ok(T data, string? message = null)
            => new ApiResponse<T> { Success = true, Data = data, Message = message, Errors = Array.Empty<string>() };

        public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null)
            => new ApiResponse<T> { Success = false, Message = message, Errors = errors ?? Array.Empty<string>() };
    }
}

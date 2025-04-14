namespace Avancira.Application.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
        public string? TraceId { get; set; }

        private ApiResponse(bool success, string? message = null, T? data = default, List<string>? errors = null, string? traceId = null)
        {
            Success = success;
            Message = message;
            Data = data;
            Errors = errors;
            TraceId = traceId;
        }

        public static ApiResponse<T> SuccessResponse(T? data = default, string? message = null)
        {
            return new ApiResponse<T>(true, message, data);
        }

        public static ApiResponse<T> Failure(string message, List<string>? errors = null, string? traceId = null)
        {
            return new ApiResponse<T>(false, message, default, errors, traceId);
        }
    }

}


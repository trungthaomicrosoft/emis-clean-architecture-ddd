namespace EMIS.BuildingBlocks.ApiResponse;

/// <summary>
/// Standard API response wrapper for all endpoints.
/// </summary>
/// <typeparam name="T">Type of the data payload</typeparam>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public ApiError? Error { get; set; }
    public ApiMetadata? Metadata { get; set; }

    public static ApiResponse<T> SuccessResult(T data, ApiMetadata? metadata = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Metadata = metadata ?? new ApiMetadata()
        };
    }

    public static ApiResponse<T> ErrorResult(string code, string message, object? details = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = new ApiError
            {
                Code = code,
                Message = message,
                Details = details
            },
            Metadata = new ApiMetadata()
        };
    }

    public static ApiResponse<T> ErrorResult(string message, object? details = null)
    {
        return ErrorResult("ERROR", message, details);
    }

    public static ApiResponse<T> SuccessResult(T data, string message, ApiMetadata? metadata = null)
    {
        var response = SuccessResult(data, metadata);
        if (response.Metadata != null)
        {
            // Could add message to metadata if needed
        }
        return response;
    }
}

public class ApiError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? Details { get; set; }
}

public class ApiMetadata
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
}

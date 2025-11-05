using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Exceptions;
using System.Net;
using System.Text.Json;

namespace Student.API.Middleware;

/// <summary>
/// Global exception handling middleware
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = exception switch
        {
            NotFoundException notFoundEx => CreateErrorResponse(
                HttpStatusCode.NotFound,
                "NOT_FOUND",
                notFoundEx.Message),

            BusinessRuleValidationException businessEx => CreateErrorResponse(
                HttpStatusCode.BadRequest,
                "BUSINESS_RULE_VIOLATION",
                businessEx.Message),

            DomainException domainEx => CreateErrorResponse(
                HttpStatusCode.BadRequest,
                "DOMAIN_ERROR",
                domainEx.Message),

            ArgumentException argEx => CreateErrorResponse(
                HttpStatusCode.BadRequest,
                "INVALID_ARGUMENT",
                argEx.Message),

            _ => CreateErrorResponse(
                HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR",
                "An internal server error occurred. Please try again later.")
        };

        response.StatusCode = (int)errorResponse.StatusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await response.WriteAsJsonAsync(errorResponse.Response, jsonOptions);
    }

    private (HttpStatusCode StatusCode, ApiResponse<object> Response) CreateErrorResponse(
        HttpStatusCode statusCode,
        string errorCode,
        string message)
    {
        return (statusCode, ApiResponse<object>.ErrorResult(errorCode, message));
    }
}

/// <summary>
/// Extension method for adding exception handling middleware
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}

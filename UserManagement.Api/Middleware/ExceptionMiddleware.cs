using System.Net;
using System.Text.Json;

namespace UserManagement.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(httpContext, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var logDetails = new
        {
            RequestMethod = context.Request.Method,
            RequestPath = context.Request.Path,
            User = context.User?.Identity?.Name ?? "Anonymous",
            ExceptionType = exception.GetType().Name,
            ExceptionMessage = exception.Message,
            StackTrace = _env.IsDevelopment() ? exception.StackTrace : null
        };

        _logger.LogError(
            exception,
            "Unhandled exception occurred | Method: {RequestMethod} | Path: {RequestPath} | User: {User} | Type: {ExceptionType}",
            logDetails.RequestMethod,
            logDetails.RequestPath,
            logDetails.User,
            logDetails.ExceptionType
        );

        (int statusCode, string message, string? detail) = exception switch
        {
            KeyNotFoundException knf => ((int)HttpStatusCode.NotFound, knf.Message, _env.IsDevelopment() ? knf.StackTrace : null),
            _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.", _env.IsDevelopment() ? exception.StackTrace : null)
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new ErrorResponse
        {
            Message = message,
            Detail = detail
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

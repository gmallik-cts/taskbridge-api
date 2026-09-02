using Microsoft.AspNetCore.Mvc;
using TaskBridge.Notifications.Services;

namespace TaskBridge.Notifications.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception exception)
        {
            var status = exception switch
            {
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                ForbiddenOperationException => StatusCodes.Status403Forbidden,
                ResourceNotFoundException => StatusCodes.Status404NotFound,
                ConflictOperationException => StatusCodes.Status409Conflict,
                ArgumentException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };
            if (status == 500) logger.LogError(exception, "Unexpected notification service error for {Method} {Path}", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = status switch { 400 => "Validation failed", 401 => "Authentication required", 403 => "Forbidden", 404 => "Resource not found", 409 => "Conflict", _ => "An unexpected error occurred" }, Detail = status == 500 ? null : exception.Message, Instance = context.Request.Path });
        }
    }
}
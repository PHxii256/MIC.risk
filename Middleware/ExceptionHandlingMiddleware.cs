using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MIC.risk.Middleware;

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
        var (statusCode, message) = exception switch
        {
            InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
            DbUpdateException => (HttpStatusCode.BadRequest, "A database update error occurred. Check your input and try again."),
            SqlException sqlEx when sqlEx.Message.Contains("Invalid column name", StringComparison.OrdinalIgnoreCase)
                => (HttpStatusCode.InternalServerError, "Database schema is out of date. Run 'dotnet ef database update' and restart the app."),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, exception.Message),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception");
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = JsonSerializer.Serialize(new { Message = message });
        await context.Response.WriteAsync(payload);
    }
}

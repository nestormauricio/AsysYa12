using System.Net;
using System.Text.Json;
using FluentValidation;
using ProviderOptimizerService.Domain.Exceptions;

namespace ProviderOptimizerService.API.Middleware;

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
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        HttpStatusCode statusCode;
        string message;
        Dictionary<string, string[]>? errors = null;

        switch (exception)
        {
            case ValidationException ve:
                statusCode = HttpStatusCode.BadRequest;
                message = "Validation failed.";
                errors = ve.Errors.ToDictionary(e => e.PropertyName, e => new[] { e.ErrorMessage });
                break;
            case ProviderNotFoundException:
                statusCode = HttpStatusCode.NotFound;
                message = exception.Message;
                break;
            case DomainException:
                statusCode = HttpStatusCode.UnprocessableEntity;
                message = exception.Message;
                break;
            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                message = exception.Message;
                break;
            default:
                statusCode = HttpStatusCode.InternalServerError;
                message = "An unexpected error occurred.";
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new { status = (int)statusCode, message, errors };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

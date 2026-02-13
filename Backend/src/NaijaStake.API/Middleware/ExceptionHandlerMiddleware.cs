using System.Net;
using System.Text.Json;
using NaijaStake.Domain.ExceptionHandling;
using NaijaStake.API.DTOs;

namespace NaijaStake.API.Middleware;

/// <summary>
/// Global exception handler middleware for consistent error responses.
/// All exceptions are caught and transformed into appropriate HTTP responses.
/// </summary>
public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
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
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            Timestamp = DateTime.UtcNow,
            Message = exception.Message
        };

        switch (exception)
        {
            case InsufficientFundsException ex:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Code = ex.ErrorCode;
                break;

            case BusinessRuleException ex:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Code = ex.ErrorCode;
                break;

            case ResourceNotFoundException ex:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Code = ex.ErrorCode;
                break;

            case InvalidStateTransitionException ex:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Code = ex.ErrorCode;
                break;

            case ConcurrencyException ex:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                response.Code = ex.ErrorCode;
                break;

            case DomainException ex:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Code = ex.ErrorCode;
                break;

            case ArgumentException ex:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Code = "VALIDATION_ERROR";
                response.Details = ex.Message;
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Code = "INTERNAL_ERROR";
                response.Message = "An unexpected error occurred.";
                response.Details = exception.Message;
                break;
        }

        return context.Response.WriteAsJsonAsync(response);
    }
}

/// <summary>
/// Extension method to register the exception handler middleware.
/// </summary>
public static class ExceptionHandlerExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlerMiddleware>();
    }
}

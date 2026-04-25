using System.Net;
using System.Text.Json;
using LibraryManagement.Api.Exceptions;

namespace LibraryManagement.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
        catch (ConflictException ex)
        {
            _logger.LogWarning(ex, "A conflict occurred.");
            await WriteErrorResponse(context, HttpStatusCode.Conflict, ex.Message);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "A resource was not found.");
            await WriteErrorResponse(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "A business rule violation occurred.");
            await WriteErrorResponse(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "A resource was not found.");
            await WriteErrorResponse(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred.");
            await WriteErrorResponse(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new { error = message };
        var json = JsonSerializer.Serialize(response);

        await context.Response.WriteAsync(json);
    }
}

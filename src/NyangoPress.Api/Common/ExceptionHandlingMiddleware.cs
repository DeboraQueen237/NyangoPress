using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NyangoPress.Shared.Common;

namespace NyangoPress.Api.Common;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict detected on {Path}", context.Request.Path);
            await WriteErrorAsync(context, StatusCodes.Status409Conflict,
                new ApiError("Concurrency.Conflict", "Cette donnée a été modifiée par un autre utilisateur. Rechargez et réessayez."));
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid business operation on {Path}", context.Request.Path);
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest,
                new ApiError("Operation.Invalid", ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Path}", context.Request.Path);
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError,
                new ApiError("Server.UnexpectedError", "Une erreur inattendue est survenue. Contactez l'administrateur."));
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, ApiError error)
    {
        if (context.Response.HasStarted) return;
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(error));
    }
}

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        return next(context);
    }
}

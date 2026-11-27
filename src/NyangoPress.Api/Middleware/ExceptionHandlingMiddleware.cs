using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace NyangoPress.Api.Middleware;

/// <summary>
/// Dernier filet : transforme toute exception en réponse JSON propre (ProblemDetails) avec un identifiant de trace,
/// sans jamais révéler de détail technique au client. Le détail est dans les journaux (Serilog).
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Le client a fermé la connexion : rien à répondre.
        }
        catch (Exception exception)
        {
            await WriteProblemAsync(context, exception);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var (status, code, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Request.Validation", "Données invalides."),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Request.Malformed", "Requête invalide."),
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Concurrency.Conflict",
                "Cette donnée vient d'être modifiée par quelqu'un d'autre. Rechargez puis réessayez."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Server.Error",
                "Une erreur est survenue. Réessayez ou contactez l'administrateur."),
        };

        if (status >= 500)
        {
            logger.LogError(exception, "Erreur non gérée (trace {TraceId})", context.TraceIdentifier);
        }
        else
        {
            logger.LogWarning(exception, "Requête refusée (trace {TraceId})", context.TraceIdentifier);
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        var problem = new ProblemDetails { Status = status, Title = title };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;

        if (exception is ValidationException validation)
        {
            problem.Extensions["errors"] = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
    }
}

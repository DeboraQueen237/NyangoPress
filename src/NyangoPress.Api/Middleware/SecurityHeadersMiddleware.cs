namespace NyangoPress.Api.Middleware;

/// <summary>En-têtes de sécurité sur chaque réponse de l'API (elle ne sert que du JSON).</summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        // La page de documentation (développement uniquement) a besoin de scripts : on ne lui applique pas la CSP stricte.
        var path = context.Request.Path;
        if (!path.StartsWithSegments("/scalar") && !path.StartsWithSegments("/openapi"))
        {
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
        }

        return next(context);
    }
}

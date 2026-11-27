using FluentValidation;

namespace NyangoPress.Api.Common;

public static class ValidationExtensions
{
    /// <summary>
    /// Valide automatiquement le paramètre de type T avec son validateur FluentValidation avant d'appeler l'endpoint ;
    /// en cas d'erreur, répond 400 avec le détail par champ. Usage : <c>.WithValidation&lt;MaRequete&gt;()</c>.
    /// </summary>
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder)
        where T : class =>
        builder.AddEndpointFilter(async (context, next) =>
        {
            var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
            var argument = context.Arguments.OfType<T>().FirstOrDefault();

            if (validator is not null && argument is not null)
            {
                var result = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
                if (!result.IsValid)
                {
                    return Results.ValidationProblem(result.ToDictionary());
                }
            }

            return await next(context);
        });
}

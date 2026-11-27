using NyangoPress.Application.Common;

namespace NyangoPress.Api.Common;

public static class ResultExtensions
{
    /// <summary>Succès : 200 avec la valeur. Échec : erreur HTTP adaptée (400, 401, 403, 404, 409).</summary>
    public static IResult ToHttpResult<T>(this Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();

    /// <summary>Succès : 204. Échec : erreur HTTP adaptée.</summary>
    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess ? Results.NoContent() : result.Error.ToProblem();

    public static IResult ToProblem(this Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };

        return Results.Problem(
            title: error.Message,
            statusCode: status,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}

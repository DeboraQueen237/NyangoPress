using NyangoPress.Application.Common;
using NyangoPress.Shared.Common;

namespace NyangoPress.Api.Common;

public static class EndpointResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return Results.Ok(result.Value);

        return MapError(result.Error);
    }

    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
            return Results.NoContent();

        return MapError(result.Error);
    }

    private static IResult MapError(Error error)
    {
        var payload = new ApiError(error.Code, error.Message);

        if (error.Code.EndsWith("NotFound", StringComparison.OrdinalIgnoreCase))
            return Results.NotFound(payload);

        if (error.Code.Contains("Forbidden", StringComparison.OrdinalIgnoreCase))
            return Results.Json(payload, statusCode: StatusCodes.Status403Forbidden);

        if (error.Code.Contains("ConcurrencyConflict", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("AlreadyOccupied", StringComparison.OrdinalIgnoreCase))
            return Results.Conflict(payload);

        return Results.BadRequest(payload);
    }
}

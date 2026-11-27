using NyangoPress.Api.Common;
using NyangoPress.Application.Common;
using NyangoPress.Application.Employees;

namespace NyangoPress.Api.Endpoints.Employees;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profile")
            .WithTags("Profil")
            .RequireAuthorization(Policies.Authenticated);

        group.MapGet("/", async Task<IResult> (ICurrentUser user, ProfileService profile, CancellationToken ct) =>
            user.UserId is { } id
                ? (await profile.GetAsync(id, ct)).ToHttpResult()
                : Results.Unauthorized());

        group.MapPut("/", async Task<IResult> (UpdateProfileRequest request, ICurrentUser user, ProfileService profile, CancellationToken ct) =>
                user.UserId is { } id
                    ? (await profile.UpdateAsync(id, request, ct)).ToHttpResult()
                    : Results.Unauthorized())
            .WithValidation<UpdateProfileRequest>();

        // Jeton Bearer (pas de cookie) : la protection anti-CSRF des formulaires n'est pas nécessaire.
        group.MapPost("/photo", async Task<IResult> (IFormFile file, ICurrentUser user, ProfileService profile, CancellationToken ct) =>
            {
                if (user.UserId is not { } id)
                {
                    return Results.Unauthorized();
                }

                await using var stream = file.OpenReadStream();
                return (await profile.SetPhotoAsync(id, stream, file.ContentType, ct)).ToHttpResult();
            })
            .DisableAntiforgery();

        group.MapGet("/photo", async Task<IResult> (ICurrentUser user, ProfileService profile, CancellationToken ct) =>
        {
            if (user.UserId is not { } id)
            {
                return Results.Unauthorized();
            }

            var result = await profile.OpenPhotoAsync(id, ct);
            return result.IsFailure
                ? result.Error.ToProblem()
                : Results.Stream(result.Value.Content, result.Value.ContentType);
        });

        return app;
    }
}

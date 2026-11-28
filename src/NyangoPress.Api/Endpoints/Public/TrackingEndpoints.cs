using Microsoft.AspNetCore.Mvc;
using NyangoPress.Api.Common;
using NyangoPress.Application.Delivery;
using NyangoPress.Shared.Delivery;

namespace NyangoPress.Api.Endpoints.Public;

public static class TrackingEndpoints
{
    public static RouteGroupBuilder MapPublicTrackingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/public")
            .WithTags("PublicPortal")
            .AllowAnonymous()
            .RequireRateLimiting("PublicTracking");

        group.MapGet("/tracking/{ticketNumber}", async (
            string ticketNumber,
            [FromQuery] string code,
            IPublicTrackingService service,
            CancellationToken ct)
            => (await service.TrackOrderAsync(ticketNumber, code, ct)).ToHttpResult());

        group.MapGet("/zones", async (IPublicTrackingService service, CancellationToken ct)
            => Results.Ok(await service.GetPublicZonesAsync(ct)));

        group.MapPost("/pickup-requests", async (
            CreatePickupRequest request,
            IPublicTrackingService service,
            CancellationToken ct) =>
        {
            var res = await service.SubmitPickupRequestAsync(request, ct);
            return res.IsSuccess
                ? Results.Created($"/api/public/pickup-requests/{res.Value!.Id}", res.Value)
                : res.ToHttpResult();
        });

        return group;
    }
}

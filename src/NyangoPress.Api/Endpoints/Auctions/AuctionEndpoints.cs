using Microsoft.AspNetCore.Mvc;
using NyangoPress.Api.Common;
using NyangoPress.Application.Auctions;
using NyangoPress.Shared.Auctions;
using NyangoPress.Shared.Common;

namespace NyangoPress.Api.Endpoints.Auctions;

public static class AuctionEndpoints
{
    public static RouteGroupBuilder MapAuctionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auctions")
            .WithTags("Auctions")
            .RequireAuthorization(Policies.AuctionsManage);

        group.MapGet("/unclaimed", async (
            [FromQuery] int? thresholdDays,
            [FromQuery] int? freeStorageDays,
            [FromQuery] decimal? dailyFeeFcfa,
            IAuctionService service,
            CancellationToken ct)
            => Results.Ok(await service.GetUnclaimedCandidatesAsync(
                thresholdDays ?? 60,
                freeStorageDays ?? 7,
                dailyFeeFcfa ?? 200m,
                ct)));

        group.MapPost("/unclaimed/detect", async (DetectUnclaimedRequest request, IAuctionService service, CancellationToken ct)
            => (await service.DetectAndMarkUnclaimedAsync(request, ct)).ToHttpResult());

        group.MapGet("/lots", async ([FromQuery] AuctionLotStatus? status, IAuctionService service, CancellationToken ct)
            => Results.Ok(await service.GetLotsAsync(status, ct)));

        group.MapGet("/lots/{id:guid}", async (Guid id, IAuctionService service, CancellationToken ct)
            => (await service.GetLotByIdAsync(id, ct)).ToHttpResult());

        group.MapPost("/lots", async (CreateAuctionLotRequest request, IAuctionService service, CancellationToken ct) =>
        {
            var res = await service.CreateLotAsync(request, ct);
            return res.IsSuccess
                ? Results.Created($"/api/auctions/lots/{res.Value!.Id}", res.Value)
                : res.ToHttpResult();
        });

        group.MapPost("/lots/{id:guid}/open", async (Guid id, IAuctionService service, CancellationToken ct)
            => (await service.OpenLotAsync(id, ct)).ToHttpResult());

        group.MapPost("/lots/{id:guid}/bids", async (Guid id, PlaceBidRequest request, IAuctionService service, CancellationToken ct)
            => (await service.PlaceBidAsync(id, request, ct)).ToHttpResult());

        group.MapPost("/lots/{id:guid}/award", async (Guid id, AwardAuctionLotRequest request, IAuctionService service, CancellationToken ct)
            => (await service.CloseAndAwardLotAsync(id, request, ct)).ToHttpResult());

        return group;
    }
}

using Microsoft.AspNetCore.Mvc;
using NyangoPress.Api.Common;
using NyangoPress.Application.Delivery;
using NyangoPress.Shared.Common;
using NyangoPress.Shared.Delivery;

namespace NyangoPress.Api.Endpoints.Delivery;

public static class DeliveryEndpoints
{
    public static RouteGroupBuilder MapDeliveryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/delivery").WithTags("Delivery");

        group.MapGet("/zones", async ([FromQuery] bool activeOnly, IDeliveryService service, CancellationToken ct)
            => Results.Ok(await service.GetZonesAsync(activeOnly, ct)))
            .RequireAuthorization(Policies.StaffOnly);

        group.MapPost("/zones", async (UpsertDeliveryZoneRequest request, IDeliveryService service, CancellationToken ct)
            => (await service.CreateZoneAsync(request, ct)).ToHttpResult())
            .RequireAuthorization(Policies.DeliveryZones);

        group.MapPut("/zones/{id:guid}", async (Guid id, UpsertDeliveryZoneRequest request, IDeliveryService service, CancellationToken ct)
            => (await service.UpdateZoneAsync(id, request, ct)).ToHttpResult())
            .RequireAuthorization(Policies.DeliveryZones);

        group.MapGet("/missions", async (
            [FromQuery] DeliveryStatus? status,
            [FromQuery] Guid? driverId,
            IDeliveryService service,
            CancellationToken ct)
            => Results.Ok(await service.GetDeliveriesAsync(status, driverId, ct)))
            .RequireAuthorization(Policies.StaffOnly);

        group.MapPost("/missions/{id:guid}/assign", async (Guid id, AssignDeliveryRequest request, IDeliveryService service, CancellationToken ct)
            => (await service.AssignDriverAsync(id, request, ct)).ToHttpResult())
            .RequireAuthorization(Policies.DeliveryManage);

        group.MapPost("/missions/{id:guid}/status", async (Guid id, UpdateDeliveryStatusRequest request, IDeliveryService service, CancellationToken ct)
            => (await service.UpdateStatusAsync(id, request, ct)).ToHttpResult())
            .RequireAuthorization(Policies.DeliveryDriver);

        group.MapPost("/missions/{id:guid}/complete", async (Guid id, CompleteDeliveryRequest request, IDeliveryService service, CancellationToken ct)
            => (await service.CompleteDeliveryAsync(id, request, ct)).ToHttpResult())
            .RequireAuthorization(Policies.DeliveryDriver);

        group.MapGet("/tours", async (
            [FromQuery] DateOnly? date,
            [FromQuery] Guid? driverId,
            IDeliveryService service,
            CancellationToken ct)
            => Results.Ok(await service.GetToursAsync(date, driverId, ct)))
            .RequireAuthorization(Policies.StaffOnly);

        group.MapPost("/tours", async (CreateTourRequest request, IDeliveryService service, CancellationToken ct)
            => (await service.CreateTourAsync(request, ct)).ToHttpResult())
            .RequireAuthorization(Policies.DeliveryManage);

        group.MapPut("/tours/{id:guid}/reorder", async (Guid id, ReorderTourStopsRequest request, IDeliveryService service, CancellationToken ct)
            => (await service.ReorderTourStopsAsync(id, request, ct)).ToHttpResult())
            .RequireAuthorization(Policies.DeliveryManage);

        group.MapPost("/tours/{id:guid}/start", async (Guid id, IDeliveryService service, CancellationToken ct)
            => (await service.StartTourAsync(id, ct)).ToHttpResult())
            .RequireAuthorization(Policies.DeliveryDriver);

        group.MapPost("/location", async (BroadcastLocationRequest request, IDeliveryService service, CancellationToken ct)
            => (await service.BroadcastLocationAsync(request, ct)).ToHttpResult())
            .RequireAuthorization(Policies.DeliveryDriver);

        group.MapGet("/pickup-requests", async ([FromQuery] PickupRequestStatus? status, IDeliveryService service, CancellationToken ct)
            => Results.Ok(await service.GetPickupRequestsAsync(status, ct)))
            .RequireAuthorization(Policies.DeliveryManage);

        group.MapPost("/pickup-requests/{id:guid}/review", async (Guid id, ReviewPickupRequest request, IDeliveryService service, CancellationToken ct)
            => (await service.ReviewPickupRequestAsync(id, request, ct)).ToHttpResult())
            .RequireAuthorization(Policies.DeliveryManage);

        return group;
    }
}

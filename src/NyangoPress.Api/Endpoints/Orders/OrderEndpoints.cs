using Microsoft.AspNetCore.Mvc;
using NyangoPress.Api.Common;
using NyangoPress.Application.Common;
using NyangoPress.Application.Orders;
using NyangoPress.Shared.Common;
using NyangoPress.Shared.Orders;

namespace NyangoPress.Api.Endpoints.Orders;

public static class OrderEndpoints
{
    public static void MapOrdersModuleEndpoints(this WebApplication app)
    {
        var catalog = app.MapGroup("/api/catalog").WithTags("CatalogOptions");
        catalog.MapGet("/options", (IPricingService pricing) => Results.Ok(new
        {
            Garments = pricing.GetGarments(),
            Services = pricing.GetServices()
        })).RequireAuthorization(Policies.StaffOnly);

        var orders = app.MapGroup("/api/orders").WithTags("Orders");

        orders.MapGet("/", async ([AsParameters] OrderQueryParams query, IOrderService service, CancellationToken ct)
            => Results.Ok(await service.SearchAsync(query, ct)))
            .RequireAuthorization(Policies.StaffOnly);

        orders.MapGet("/{id:guid}", async (Guid id, IOrderService service, CancellationToken ct)
            => (await service.GetByIdAsync(id, ct)).ToHttpResult())
            .RequireAuthorization(Policies.StaffOnly);

        orders.MapGet("/by-ticket/{ticketNumber}", async (string ticketNumber, IOrderService service, CancellationToken ct)
            => (await service.GetByTicketAsync(ticketNumber, ct)).ToHttpResult())
            .RequireAuthorization(Policies.StaffOnly);

        orders.MapPost("/preview-price", async (OrderPricePreviewRequest request, IOrderService service, CancellationToken ct)
            => (await service.PreviewPriceAsync(request, ct)).ToHttpResult())
            .RequireAuthorization(Policies.OrdersCreate);

        orders.MapPost("/estimate-pickup-date", async (EstimatePickupDateRequest request, IOrderService service, CancellationToken ct)
            => Results.Ok(await service.EstimatePickupDateAsync(request, ct)))
            .RequireAuthorization(Policies.OrdersCreate);

        orders.MapPost("/", async (CreateOrderRequest request, IOrderService service, CancellationToken ct) =>
        {
            var res = await service.CreateAsync(request, ct);
            return res.IsSuccess
                ? Results.Created($"/api/orders/{res.Value!.Id}", res.Value)
                : res.ToHttpResult();
        }).RequireAuthorization(Policies.OrdersCreate);

        orders.MapPut("/{orderId:guid}/items/{itemId:guid}/inspection", async (
            Guid orderId,
            Guid itemId,
            UpdateOrderItemInspectionRequest request,
            IOrderService service,
            CancellationToken ct)
            => (await service.UpdateInspectionAsync(orderId, itemId, request, ct)).ToHttpResult())
            .RequireAuthorization(Policies.OrdersCreate);

        orders.MapPost("/{orderId:guid}/items/{itemId:guid}/photos", async (
            Guid orderId,
            Guid itemId,
            AddItemPhotoRequest request,
            IOrderService service,
            CancellationToken ct)
            => (await service.AddItemPhotoAsync(orderId, itemId, request, ct)).ToHttpResult())
            .RequireAuthorization(Policies.OrdersCreate);

        orders.MapPost("/{id:guid}/pickup", async (Guid id, PickupOrderRequest request, IOrderService service, CancellationToken ct)
            => (await service.PickupAsync(id, request, ct)).ToHttpResult())
            .RequireAuthorization(Policies.OrdersCreate);

        orders.MapPost("/{id:guid}/cancel", async (Guid id, CancelOrderRequest request, IOrderService service, CancellationToken ct)
            => (await service.CancelAsync(id, request, ct)).ToHttpResult())
            .RequireAuthorization(Policies.OrdersCancel);

        orders.MapPost("/{id:guid}/claims", async (Guid id, CreateClaimRequest request, IOrderService service, CancellationToken ct)
            => (await service.OpenClaimAsync(id, request, ct)).ToHttpResult())
            .RequireAuthorization(Policies.OrdersCreate);

        orders.MapPut("/claims/{claimId:guid}/resolve", async (Guid claimId, ResolveClaimRequest request, IOrderService service, CancellationToken ct)
            => (await service.ResolveClaimAsync(claimId, request, ct)).ToHttpResult())
            .RequireAuthorization(Policies.CustomersAdmin);

        var prod = app.MapGroup("/api/production").WithTags("Production");

        prod.MapGet("/queue", async (
            [FromQuery] ItemStatus? status,
            [FromQuery] string? serviceCode,
            IOrderService service,
            CancellationToken ct)
            => Results.Ok(await service.GetProductionQueueAsync(status, serviceCode, ct)))
            .RequireAuthorization(Policies.ProductionAdvance);

        prod.MapPost("/items/{itemId:guid}/status", async (
            Guid itemId,
            AdvanceItemStatusRequest request,
            IOrderService service,
            CancellationToken ct)
            => (await service.AdvanceItemStatusAsync(itemId, request, ct)).ToHttpResult())
            .RequireAuthorization(Policies.ProductionAdvance);

        prod.MapPost("/scan", async (QrScanRequest request, IOrderService service, CancellationToken ct)
            => (await service.ScanQrAsync(request, ct)).ToHttpResult())
            .RequireAuthorization(Policies.ProductionAdvance);

        prod.MapGet("/items/{qrCode}/qr.svg", (string qrCode, IQrCodeService qrService) =>
        {
            var svg = qrService.GenerateSvg(qrCode);
            return Results.Content(svg, "image/svg+xml");
        }).RequireAuthorization(Policies.StaffOnly);

        var storage = app.MapGroup("/api/storage").WithTags("Storage");

        storage.MapGet("/locations", async (
            [FromQuery] bool? occupiedOnly,
            [FromQuery] string? search,
            IOrderService service,
            CancellationToken ct)
            => Results.Ok(await service.GetStorageLocationsAsync(occupiedOnly, search, ct)))
            .RequireAuthorization(Policies.ProductionAdvance);

        storage.MapPost("/locations", async (CreateStorageLocationRequest request, IOrderService service, CancellationToken ct)
            => (await service.CreateStorageLocationAsync(request, ct)).ToHttpResult())
            .RequireAuthorization(Policies.ProductionAdvance);

        storage.MapPost("/items/{itemId:guid}/assign", async (Guid itemId, AssignStorageRequest request, IOrderService service, CancellationToken ct)
            => (await service.AssignStorageAsync(itemId, request, ct)).ToHttpResult())
            .RequireAuthorization(Policies.ProductionAdvance);
    }
}

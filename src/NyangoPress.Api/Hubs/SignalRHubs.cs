using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NyangoPress.Application.Common;

namespace NyangoPress.Api.Hubs;

[Authorize]
public sealed class OrdersHub : Hub
{
    public async Task JoinProductionRoom()
        => await Groups.AddToGroupAsync(Context.ConnectionId, "production");

    public async Task JoinOrderRoom(Guid orderId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"order:{orderId}");
}

public sealed class DeliveryHub : Hub
{
    public async Task JoinTrackingRoom(string ticketNumber)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"tracking:{ticketNumber.Trim().ToUpperInvariant()}");

    public async Task JoinDispatchRoom()
        => await Groups.AddToGroupAsync(Context.ConnectionId, "dispatch");
}

public sealed class SignalRRealtimeNotifier(
    IHubContext<OrdersHub> ordersHub,
    IHubContext<DeliveryHub> deliveryHub) : IRealtimeNotifier
{
    public async Task NotifyOrderUpdatedAsync(Guid orderId, string ticketNumber, CancellationToken ct = default)
    {
        await ordersHub.Clients.Group("production").SendAsync("OrderUpdated", orderId, ticketNumber, ct);
        await ordersHub.Clients.Group($"order:{orderId}").SendAsync("OrderUpdated", orderId, ticketNumber, ct);
        await deliveryHub.Clients.Group($"tracking:{ticketNumber}").SendAsync("TrackingUpdated", ticketNumber, ct);
    }

    public async Task NotifyProductionItemChangedAsync(Guid orderId, Guid itemId, string newStatus, CancellationToken ct = default)
    {
        await ordersHub.Clients.Group("production").SendAsync("ProductionItemChanged", orderId, itemId, newStatus, ct);
        await ordersHub.Clients.Group($"order:{orderId}").SendAsync("ProductionItemChanged", orderId, itemId, newStatus, ct);
    }

    public async Task NotifyDriverLocationAsync(
        Guid driverId,
        string driverName,
        Guid? deliveryOrderId,
        string? ticketNumber,
        double latitude,
        double longitude,
        DateTimeOffset recordedAt,
        CancellationToken ct = default)
    {
        var payload = new { driverId, driverName, deliveryOrderId, ticketNumber, latitude, longitude, recordedAt };
        await deliveryHub.Clients.Group("dispatch").SendAsync("DriverLocationUpdated", payload, ct);
        if (!string.IsNullOrWhiteSpace(ticketNumber))
        {
            await deliveryHub.Clients.Group($"tracking:{ticketNumber}").SendAsync("DriverLocationUpdated", payload, ct);
        }
    }

    public async Task NotifyDeliveryStatusChangedAsync(Guid deliveryOrderId, string ticketNumber, string status, CancellationToken ct = default)
    {
        await deliveryHub.Clients.Group("dispatch").SendAsync("DeliveryStatusChanged", deliveryOrderId, ticketNumber, status, ct);
        if (!string.IsNullOrWhiteSpace(ticketNumber))
        {
            await deliveryHub.Clients.Group($"tracking:{ticketNumber}").SendAsync("TrackingUpdated", ticketNumber, ct);
        }
    }
}

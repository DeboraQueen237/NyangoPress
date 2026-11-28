using Microsoft.AspNetCore.SignalR.Client;

namespace NyangoPress.Web.Services;

public sealed class SignalRService(IConfiguration configuration, AuthSessionService session) : IAsyncDisposable
{
    private HubConnection? _ordersHub;
    private HubConnection? _deliveryHub;

    public event Action? OnProductionChanged;
    public event Action<string>? OnTrackingUpdated;
    public event Action<double, double, string>? OnDriverLocationUpdated;

    private string ApiBaseUrl => configuration["ApiBaseUrl"]?.TrimEnd('/') ?? "http://localhost:5059";

    public async Task ConnectOrdersHubAsync()
    {
        if (string.IsNullOrWhiteSpace(session.AccessToken)) return;
        if (_ordersHub is { State: HubConnectionState.Connected }) return;

        _ordersHub = new HubConnectionBuilder()
            .WithUrl($"{ApiBaseUrl}/hubs/orders", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(session.AccessToken);
            })
            .WithAutomaticReconnect()
            .Build();

        _ordersHub.On<Guid, Guid, string>("ProductionItemChanged", (_, _, _) => OnProductionChanged?.Invoke());
        _ordersHub.On<Guid, string>("OrderUpdated", (_, _) => OnProductionChanged?.Invoke());

        try
        {
            await _ordersHub.StartAsync();
            await _ordersHub.InvokeAsync("JoinProductionRoom");
        }
        catch
        {
            // Resilient fallback if SignalR server is unreachable during offline test
        }
    }

    public async Task ConnectTrackingHubAsync(string ticketNumber)
    {
        if (_deliveryHub is null)
        {
            _deliveryHub = new HubConnectionBuilder()
                .WithUrl($"{ApiBaseUrl}/hubs/delivery")
                .WithAutomaticReconnect()
                .Build();

            _deliveryHub.On<string>("TrackingUpdated", ticket => OnTrackingUpdated?.Invoke(ticket));
            _deliveryHub.On<System.Text.Json.JsonElement>("DriverLocationUpdated", payload =>
            {
                if (payload.TryGetProperty("latitude", out var latProp) &&
                    payload.TryGetProperty("longitude", out var lngProp))
                {
                    var name = payload.TryGetProperty("driverName", out var n) ? n.GetString() ?? "Livreur" : "Livreur";
                    OnDriverLocationUpdated?.Invoke(latProp.GetDouble(), lngProp.GetDouble(), name);
                }
            });
        }

        try
        {
            if (_deliveryHub.State != HubConnectionState.Connected)
                await _deliveryHub.StartAsync();
            await _deliveryHub.InvokeAsync("JoinTrackingRoom", ticketNumber);
        }
        catch
        {
            // Ignore transient reconnect error
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_ordersHub is not null) await _ordersHub.DisposeAsync();
        if (_deliveryHub is not null) await _deliveryHub.DisposeAsync();
    }
}

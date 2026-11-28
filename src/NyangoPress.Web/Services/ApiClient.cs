using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using NyangoPress.Shared.Auctions;
using NyangoPress.Shared.Common;
using NyangoPress.Shared.Customers;
using NyangoPress.Shared.Delivery;
using NyangoPress.Shared.Orders;
using NyangoPress.Shared.Tracking;

namespace NyangoPress.Web.Services;

public sealed record ApiCallResult<T>(bool IsSuccess, T? Data, ApiError? Error)
{
    public static ApiCallResult<T> Ok(T? data) => new(true, data, null);
    public static ApiCallResult<T> Fail(ApiError error) => new(false, default, error);
}

public sealed record CatalogOptionsResponse(
    IReadOnlyList<GarmentOptionDto> Garments,
    IReadOnlyList<ServiceOptionDto> Services);

public sealed class ApiClient(HttpClient http, AuthSessionService session)
{
    private void ApplyAuth()
    {
        if (!string.IsNullOrWhiteSpace(session.AccessToken))
        {
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", session.AccessToken);
        }
        else
        {
            http.DefaultRequestHeaders.Authorization = null;
        }
    }

    private async Task<ApiCallResult<T>> SendAsync<T>(HttpRequestMessage req, CancellationToken ct = default)
    {
        ApplyAuth();
        try
        {
            using var response = await http.SendAsync(req, ct);
            if (response.IsSuccessStatusCode)
            {
                if (typeof(T) == typeof(bool) && response.Content.Headers.ContentLength == 0)
                    return ApiCallResult<T>.Ok((T)(object)true);

                var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
                return ApiCallResult<T>.Ok(data);
            }

            ApiError? apiError = null;
            try
            {
                apiError = await response.Content.ReadFromJsonAsync<ApiError>(cancellationToken: ct);
            }
            catch
            {
                // Ignore non-JSON bodies
            }

            apiError ??= new ApiError(
                $"HTTP.{(int)response.StatusCode}",
                response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? "Session expirée ou non authentifiée."
                    : response.StatusCode == System.Net.HttpStatusCode.Forbidden
                        ? "Accès refusé pour votre rôle."
                        : $"Erreur serveur ({(int)response.StatusCode}).");

            return ApiCallResult<T>.Fail(apiError);
        }
        catch (Exception ex)
        {
            return ApiCallResult<T>.Fail(new ApiError("Network.Error", $"Connexion API impossible : {ex.Message}"));
        }
    }

    // Auth
    public async Task<ApiCallResult<AuthResponse>> DemoLoginAsync(string roleCode, string? displayName = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/demo-login")
        {
            Content = JsonContent.Create(new DemoLoginRequest(roleCode, displayName))
        };
        var res = await SendAsync<AuthResponse>(req);
        if (res.IsSuccess && res.Data is not null) session.SetSession(res.Data);
        return res;
    }

    public async Task<ApiCallResult<AuthResponse>> LoginAsync(string identifier, string password)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new LoginRequest(identifier, password))
        };
        var res = await SendAsync<AuthResponse>(req);
        if (res.IsSuccess && res.Data is not null) session.SetSession(res.Data);
        return res;
    }

    // Customers
    public Task<ApiCallResult<PagedResult<CustomerSummaryDto>>> GetCustomersAsync(CustomerQueryParams q)
    {
        var url = QueryHelpers.AddQueryString("/api/customers", new Dictionary<string, string?>
        {
            ["search"] = q.Search,
            ["type"] = q.Type?.ToString(),
            ["page"] = q.Page.ToString(),
            ["pageSize"] = q.PageSize.ToString(),
            ["sortBy"] = q.SortBy,
            ["sortDesc"] = q.SortDesc.ToString()
        });
        return SendAsync<PagedResult<CustomerSummaryDto>>(new HttpRequestMessage(HttpMethod.Get, url));
    }

    public Task<ApiCallResult<CustomerDto>> GetCustomerAsync(Guid id)
        => SendAsync<CustomerDto>(new HttpRequestMessage(HttpMethod.Get, $"/api/customers/{id}"));

    public Task<ApiCallResult<CustomerHistoryDto>> GetCustomerHistoryAsync(Guid id)
        => SendAsync<CustomerHistoryDto>(new HttpRequestMessage(HttpMethod.Get, $"/api/customers/{id}/history"));

    public Task<ApiCallResult<IReadOnlyList<DuplicateMatchDto>>> CheckCustomerDuplicatesAsync(string fullName, string phone, Guid? excludeId = null)
    {
        var url = QueryHelpers.AddQueryString("/api/customers/duplicates", new Dictionary<string, string?>
        {
            ["fullName"] = fullName,
            ["phone"] = phone,
            ["excludeId"] = excludeId?.ToString()
        });
        return SendAsync<IReadOnlyList<DuplicateMatchDto>>(new HttpRequestMessage(HttpMethod.Get, url));
    }

    public Task<ApiCallResult<CustomerDto>> CreateCustomerAsync(CreateCustomerRequest request)
        => SendAsync<CustomerDto>(new HttpRequestMessage(HttpMethod.Post, "/api/customers") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<CustomerDto>> UpdateCustomerAsync(Guid id, UpdateCustomerRequest request)
        => SendAsync<CustomerDto>(new HttpRequestMessage(HttpMethod.Put, $"/api/customers/{id}") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<CustomerDto>> UpdateCustomerPreferencesAsync(Guid id, UpdateContactPreferenceRequest request)
        => SendAsync<CustomerDto>(new HttpRequestMessage(HttpMethod.Put, $"/api/customers/{id}/preferences") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<CustomerDto>> UpsertCompanyContractAsync(Guid id, UpsertCompanyContractRequest request)
        => SendAsync<CustomerDto>(new HttpRequestMessage(HttpMethod.Put, $"/api/customers/{id}/contract") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<CustomerDto>> MergeCustomersAsync(MergeCustomersRequest request)
        => SendAsync<CustomerDto>(new HttpRequestMessage(HttpMethod.Post, "/api/customers/merge") { Content = JsonContent.Create(request) });

    public async Task<byte[]?> DownloadBytesAsync(string url)
    {
        ApplyAuth();
        using var resp = await http.GetAsync(url);
        return resp.IsSuccessStatusCode ? await resp.Content.ReadAsByteArrayAsync() : null;
    }

    public async Task<ApiCallResult<CustomerImportResultDto>> ImportCustomersAsync(Stream stream, string fileName)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "file", fileName);
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/customers/import") { Content = content };
        return await SendAsync<CustomerImportResultDto>(req);
    }

    // Orders & Catalog & Production & Storage
    public Task<ApiCallResult<CatalogOptionsResponse>> GetCatalogOptionsAsync()
        => SendAsync<CatalogOptionsResponse>(new HttpRequestMessage(HttpMethod.Get, "/api/catalog/options"));

    public Task<ApiCallResult<PagedResult<OrderSummaryDto>>> GetOrdersAsync(OrderQueryParams q)
    {
        var url = QueryHelpers.AddQueryString("/api/orders", new Dictionary<string, string?>
        {
            ["search"] = q.Search,
            ["status"] = q.Status?.ToString(),
            ["expressOnly"] = q.ExpressOnly?.ToString(),
            ["customerId"] = q.CustomerId?.ToString(),
            ["page"] = q.Page.ToString(),
            ["pageSize"] = q.PageSize.ToString()
        });
        return SendAsync<PagedResult<OrderSummaryDto>>(new HttpRequestMessage(HttpMethod.Get, url));
    }

    public Task<ApiCallResult<OrderDto>> GetOrderAsync(Guid id)
        => SendAsync<OrderDto>(new HttpRequestMessage(HttpMethod.Get, $"/api/orders/{id}"));

    public Task<ApiCallResult<OrderPricePreviewDto>> PreviewOrderPriceAsync(OrderPricePreviewRequest request)
        => SendAsync<OrderPricePreviewDto>(new HttpRequestMessage(HttpMethod.Post, "/api/orders/preview-price") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<EstimatePickupDateResponse>> EstimatePickupDateAsync(EstimatePickupDateRequest request)
        => SendAsync<EstimatePickupDateResponse>(new HttpRequestMessage(HttpMethod.Post, "/api/orders/estimate-pickup-date") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<OrderDto>> CreateOrderAsync(CreateOrderRequest request)
        => SendAsync<OrderDto>(new HttpRequestMessage(HttpMethod.Post, "/api/orders") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<OrderDto>> PickupOrderAsync(Guid orderId, PickupOrderRequest request)
        => SendAsync<OrderDto>(new HttpRequestMessage(HttpMethod.Post, $"/api/orders/{orderId}/pickup") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<OrderDto>> CancelOrderAsync(Guid orderId, CancelOrderRequest request)
        => SendAsync<OrderDto>(new HttpRequestMessage(HttpMethod.Post, $"/api/orders/{orderId}/cancel") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<ClaimDto>> OpenClaimAsync(Guid orderId, CreateClaimRequest request)
        => SendAsync<ClaimDto>(new HttpRequestMessage(HttpMethod.Post, $"/api/orders/{orderId}/claims") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<ClaimDto>> ResolveClaimAsync(Guid claimId, ResolveClaimRequest request)
        => SendAsync<ClaimDto>(new HttpRequestMessage(HttpMethod.Put, $"/api/orders/claims/{claimId}/resolve") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<IReadOnlyList<ProductionItemCardDto>>> GetProductionQueueAsync(ItemStatus? status = null, string? serviceCode = null)
    {
        var url = QueryHelpers.AddQueryString("/api/production/queue", new Dictionary<string, string?>
        {
            ["status"] = status?.ToString(),
            ["serviceCode"] = serviceCode
        });
        return SendAsync<IReadOnlyList<ProductionItemCardDto>>(new HttpRequestMessage(HttpMethod.Get, url));
    }

    public Task<ApiCallResult<OrderItemDto>> AdvanceItemStatusAsync(Guid itemId, AdvanceItemStatusRequest request)
        => SendAsync<OrderItemDto>(new HttpRequestMessage(HttpMethod.Post, $"/api/production/items/{itemId}/status") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<QrScanResultDto>> ScanQrAsync(QrScanRequest request)
        => SendAsync<QrScanResultDto>(new HttpRequestMessage(HttpMethod.Post, "/api/production/scan") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<IReadOnlyList<StorageLocationDto>>> GetStorageLocationsAsync(bool? occupiedOnly = null, string? search = null)
    {
        var url = QueryHelpers.AddQueryString("/api/storage/locations", new Dictionary<string, string?>
        {
            ["occupiedOnly"] = occupiedOnly?.ToString(),
            ["search"] = search
        });
        return SendAsync<IReadOnlyList<StorageLocationDto>>(new HttpRequestMessage(HttpMethod.Get, url));
    }

    public Task<ApiCallResult<StorageLocationDto>> CreateStorageLocationAsync(CreateStorageLocationRequest request)
        => SendAsync<StorageLocationDto>(new HttpRequestMessage(HttpMethod.Post, "/api/storage/locations") { Content = JsonContent.Create(request) });

    // Delivery & Public Portal
    public Task<ApiCallResult<IReadOnlyList<DeliveryZoneDto>>> GetDeliveryZonesAsync(bool activeOnly = false)
        => SendAsync<IReadOnlyList<DeliveryZoneDto>>(new HttpRequestMessage(HttpMethod.Get, $"/api/delivery/zones?activeOnly={activeOnly}"));

    public Task<ApiCallResult<DeliveryZoneDto>> CreateDeliveryZoneAsync(UpsertDeliveryZoneRequest request)
        => SendAsync<DeliveryZoneDto>(new HttpRequestMessage(HttpMethod.Post, "/api/delivery/zones") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<DeliveryZoneDto>> UpdateDeliveryZoneAsync(Guid id, UpsertDeliveryZoneRequest request)
        => SendAsync<DeliveryZoneDto>(new HttpRequestMessage(HttpMethod.Put, $"/api/delivery/zones/{id}") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<IReadOnlyList<DeliveryOrderDto>>> GetDeliveriesAsync(DeliveryStatus? status = null, Guid? driverId = null)
    {
        var url = QueryHelpers.AddQueryString("/api/delivery/missions", new Dictionary<string, string?>
        {
            ["status"] = status?.ToString(),
            ["driverId"] = driverId?.ToString()
        });
        return SendAsync<IReadOnlyList<DeliveryOrderDto>>(new HttpRequestMessage(HttpMethod.Get, url));
    }

    public Task<ApiCallResult<DeliveryOrderDto>> AssignDeliveryAsync(Guid deliveryId, AssignDeliveryRequest request)
        => SendAsync<DeliveryOrderDto>(new HttpRequestMessage(HttpMethod.Post, $"/api/delivery/missions/{deliveryId}/assign") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<DeliveryOrderDto>> UpdateDeliveryStatusAsync(Guid deliveryId, UpdateDeliveryStatusRequest request)
        => SendAsync<DeliveryOrderDto>(new HttpRequestMessage(HttpMethod.Post, $"/api/delivery/missions/{deliveryId}/status") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<DeliveryOrderDto>> CompleteDeliveryAsync(Guid deliveryId, CompleteDeliveryRequest request)
        => SendAsync<DeliveryOrderDto>(new HttpRequestMessage(HttpMethod.Post, $"/api/delivery/missions/{deliveryId}/complete") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<IReadOnlyList<DeliveryTourDto>>> GetDeliveryToursAsync()
        => SendAsync<IReadOnlyList<DeliveryTourDto>>(new HttpRequestMessage(HttpMethod.Get, "/api/delivery/tours"));

    public Task<ApiCallResult<DeliveryTourDto>> CreateTourAsync(CreateTourRequest request)
        => SendAsync<DeliveryTourDto>(new HttpRequestMessage(HttpMethod.Post, "/api/delivery/tours") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<DeliveryTourDto>> ReorderTourStopsAsync(Guid tourId, ReorderTourStopsRequest request)
        => SendAsync<DeliveryTourDto>(new HttpRequestMessage(HttpMethod.Put, $"/api/delivery/tours/{tourId}/reorder") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<DeliveryTourDto>> StartTourAsync(Guid tourId)
        => SendAsync<DeliveryTourDto>(new HttpRequestMessage(HttpMethod.Post, $"/api/delivery/tours/{tourId}/start"));

    public Task<ApiCallResult<DriverLocationDto>> BroadcastLocationAsync(BroadcastLocationRequest request)
        => SendAsync<DriverLocationDto>(new HttpRequestMessage(HttpMethod.Post, "/api/delivery/location") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<IReadOnlyList<PickupRequestDto>>> GetPickupRequestsAsync()
        => SendAsync<IReadOnlyList<PickupRequestDto>>(new HttpRequestMessage(HttpMethod.Get, "/api/delivery/pickup-requests"));

    public Task<ApiCallResult<PickupRequestDto>> ReviewPickupRequestAsync(Guid id, ReviewPickupRequest request)
        => SendAsync<PickupRequestDto>(new HttpRequestMessage(HttpMethod.Post, $"/api/delivery/pickup-requests/{id}/review") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<PublicOrderTrackingDto>> TrackPublicOrderAsync(string ticketNumber, string code)
        => SendAsync<PublicOrderTrackingDto>(new HttpRequestMessage(HttpMethod.Get, $"/api/public/tracking/{Uri.EscapeDataString(ticketNumber)}?code={Uri.EscapeDataString(code)}"));

    public Task<ApiCallResult<IReadOnlyList<DeliveryZoneDto>>> GetPublicZonesAsync()
        => SendAsync<IReadOnlyList<DeliveryZoneDto>>(new HttpRequestMessage(HttpMethod.Get, "/api/public/zones"));

    public Task<ApiCallResult<PublicPickupRequestResultDto>> SubmitPublicPickupRequestAsync(CreatePickupRequest request)
        => SendAsync<PublicPickupRequestResultDto>(new HttpRequestMessage(HttpMethod.Post, "/api/public/pickup-requests") { Content = JsonContent.Create(request) });

    // Auctions
    public Task<ApiCallResult<IReadOnlyList<UnclaimedCandidateDto>>> GetUnclaimedCandidatesAsync()
        => SendAsync<IReadOnlyList<UnclaimedCandidateDto>>(new HttpRequestMessage(HttpMethod.Get, "/api/auctions/unclaimed"));

    public Task<ApiCallResult<DetectUnclaimedResultDto>> DetectUnclaimedAsync(DetectUnclaimedRequest request)
        => SendAsync<DetectUnclaimedResultDto>(new HttpRequestMessage(HttpMethod.Post, "/api/auctions/unclaimed/detect") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<IReadOnlyList<AuctionLotDto>>> GetAuctionLotsAsync()
        => SendAsync<IReadOnlyList<AuctionLotDto>>(new HttpRequestMessage(HttpMethod.Get, "/api/auctions/lots"));

    public Task<ApiCallResult<AuctionLotDto>> CreateAuctionLotAsync(CreateAuctionLotRequest request)
        => SendAsync<AuctionLotDto>(new HttpRequestMessage(HttpMethod.Post, "/api/auctions/lots") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<AuctionLotDto>> OpenAuctionLotAsync(Guid lotId)
        => SendAsync<AuctionLotDto>(new HttpRequestMessage(HttpMethod.Post, $"/api/auctions/lots/{lotId}/open"));

    public Task<ApiCallResult<AuctionLotDto>> PlaceBidAsync(Guid lotId, PlaceBidRequest request)
        => SendAsync<AuctionLotDto>(new HttpRequestMessage(HttpMethod.Post, $"/api/auctions/lots/{lotId}/bids") { Content = JsonContent.Create(request) });

    public Task<ApiCallResult<AuctionLotDto>> AwardAuctionLotAsync(Guid lotId, AwardAuctionLotRequest request)
        => SendAsync<AuctionLotDto>(new HttpRequestMessage(HttpMethod.Post, $"/api/auctions/lots/{lotId}/award") { Content = JsonContent.Create(request) });
}

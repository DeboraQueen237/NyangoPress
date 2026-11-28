using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using NyangoPress.Shared.Auctions;
using NyangoPress.Shared.Common;
using NyangoPress.Shared.Customers;
using NyangoPress.Shared.Delivery;
using NyangoPress.Shared.Orders;
using NyangoPress.Shared.Tracking;

namespace NyangoPress.IntegrationTests.Orders;

public sealed class NyangoPressApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"nyangopress-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Sqlite",
                ["ConnectionStrings:Sqlite"] = $"Data Source={_dbPath}"
            });
        });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { }
        }
    }
}

public class Dev2EndToEndTests(NyangoPressApiFactory factory) : IClassFixture<NyangoPressApiFactory>
{
    private async Task<HttpClient> CreateAuthorizedClientAsync(string role)
    {
        var client = factory.CreateClient();
        var authResp = await client.PostAsJsonAsync("/api/auth/demo-login", new DemoLoginRequest(role));
        authResp.EnsureSuccessStatusCode();
        var auth = await authResp.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    [Fact]
    public async Task AuthorizationMatrix_EnforcesRoleBoundaries_Returns401And403()
    {
        var anon = factory.CreateClient();
        (await anon.GetAsync("/api/customers")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anon.GetAsync("/api/orders")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var operatorClient = await CreateAuthorizedClientAsync(Roles.Operator);
        var createCustResp = await operatorClient.PostAsJsonAsync("/api/customers",
            new CreateCustomerRequest(CustomerType.Individual, "Test Forbidden", "+237699999999", null, null, null, null));
        createCustResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var cashierClient = await CreateAuthorizedClientAsync(Roles.Cashier);
        (await cashierClient.GetAsync("/api/auctions/lots")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var driverClient = await CreateAuthorizedClientAsync(Roles.Driver);
        var zoneResp = await driverClient.PostAsJsonAsync("/api/delivery/zones",
            new UpsertDeliveryZoneRequest("Zone X", "Q1", 1000m, "09:00", 2, true));
        zoneResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FullOrderLifecycle_CreateScanStoragePickupAndConcurrency_WorksEndToEnd()
    {
        var cashier = await CreateAuthorizedClientAsync(Roles.Cashier);
        var op = await CreateAuthorizedClientAsync(Roles.Operator);
        var manager = await CreateAuthorizedClientAsync(Roles.Manager);

        // 1. Create Customer
        var custResp = await cashier.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            CustomerType.Individual,
            "Jean-Paul Biya Test",
            "+237690887766",
            "+237690887766",
            "jp@exemple.cm",
            "Bastos, Yaoundé",
            null));
        custResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var customer = await custResp.Content.ReadFromJsonAsync<CustomerDto>();
        customer.Should().NotBeNull();

        // 2. Duplicate check blocks duplicate creation without IgnoreDuplicateWarning
        var dupResp = await cashier.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            CustomerType.Individual, "Jean-Paul Biya Test", "+237690887766", null, null, null, null));
        dupResp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 3. Create Storage Location
        var locResp = await op.PostAsJsonAsync("/api/storage/locations", new CreateStorageLocationRequest("Z", "R9", "99"));
        locResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var loc = await locResp.Content.ReadFromJsonAsync<StorageLocationDto>();

        // 4. Create Order (deposit 1000 FCFA)
        var orderResp = await cashier.PostAsJsonAsync("/api/orders", new CreateOrderRequest(
            customer!.Id,
            DeliveryOption.StorePickup,
            null,
            null,
            IsExpress: true,
            PromisedAt: null,
            InitialDepositFcfa: 1000m,
            PromoCode: null,
            AcceptedTermsAndConditions: true,
            Notes: "Test intégration",
            Items:
            [
                new CreateOrderItemRequest("SHIRT", "WASH_IRON", true, false, "Blanc", "Lacoste", ["Bouton manquant"], "Manche droite", null)
            ]));
        orderResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await orderResp.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();
        order!.Items.Should().HaveCount(1);
        order.BalanceFcfa.Should().BeGreaterThan(0m);
        var item = order.Items[0];

        // 5. Public tracking endpoint hides sensitive PII and requires valid tracking code
        var anon = factory.CreateClient();
        (await anon.GetAsync($"/api/public/tracking/{order.TicketNumber}?code=WRONG")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var trackResp = await anon.GetAsync($"/api/public/tracking/{order.TicketNumber}?code={order.TrackingCode}");
        trackResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var tracking = await trackResp.Content.ReadFromJsonAsync<PublicOrderTrackingDto>();
        tracking!.CustomerMaskedName.Should().NotBe(customer.FullName);

        // 6. Operator scans QR through workflow: Received -> Sorted -> InTreatment -> QualityCheck -> Ready (stored in Z-R9-99)
        var scan1 = await op.PostAsJsonAsync("/api/production/scan", new QrScanRequest(item.QrCode));
        scan1.EnsureSuccessStatusCode();
        (await scan1.Content.ReadFromJsonAsync<QrScanResultDto>())!.CurrentStatus.Should().Be(ItemStatus.Sorted);

        var scan2 = await op.PostAsJsonAsync("/api/production/scan", new QrScanRequest(item.QrCode));
        (await scan2.Content.ReadFromJsonAsync<QrScanResultDto>())!.CurrentStatus.Should().Be(ItemStatus.InTreatment);

        var scan3 = await op.PostAsJsonAsync("/api/production/scan", new QrScanRequest(item.QrCode));
        (await scan3.Content.ReadFromJsonAsync<QrScanResultDto>())!.CurrentStatus.Should().Be(ItemStatus.QualityCheck);

        var readyResp = await op.PostAsJsonAsync($"/api/production/items/{item.Id}/status",
            new AdvanceItemStatusRequest(ItemStatus.Ready, StorageLocationId: loc!.Id));
        readyResp.EnsureSuccessStatusCode();
        var readyItem = await readyResp.Content.ReadFromJsonAsync<OrderItemDto>();
        readyItem!.StorageLocationCode.Should().Be("Z-R9-99");

        // 7. Optimistic Concurrency: simulate stale RowVersion on Customer update
        var staleUpdate = await cashier.PutAsJsonAsync($"/api/customers/{customer.Id}", new UpdateCustomerRequest(
            CustomerType.Individual, "Stale Name", "+237690887766", null, null, null, null, RowVersion: 999));
        staleUpdate.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 8. RG-03: Pickup blocked when balance unpaid without override
        var blockedPickup = await cashier.PostAsJsonAsync($"/api/orders/{order.Id}/pickup",
            new PickupOrderRequest(0m, null, OverrideUnpaidBalance: false));
        blockedPickup.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 9. Cashier collects remaining balance + digital signature -> Pickup succeeds!
        var freshOrder = await (await cashier.GetAsync($"/api/orders/{order.Id}")).Content.ReadFromJsonAsync<OrderDto>();
        var okPickup = await cashier.PostAsJsonAsync($"/api/orders/{order.Id}/pickup",
            new PickupOrderRequest(freshOrder!.BalanceFcfa, null, OverrideUnpaidBalance: false, RowVersion: freshOrder.RowVersion));
        okPickup.StatusCode.Should().Be(HttpStatusCode.OK);
        var pickedOrder = await okPickup.Content.ReadFromJsonAsync<OrderDto>();
        pickedOrder!.Status.Should().Be(OrderStatus.Delivered);
        pickedOrder.BalanceFcfa.Should().Be(0m);

        // 10. Manager creates & awards Auction on unclaimed items (verify endpoint flow)
        var detectResp = await manager.PostAsJsonAsync("/api/auctions/unclaimed/detect",
            new DetectUnclaimedRequest(60, 7, 200m, ApplyStatusTransition: true));
        detectResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeliveryWorkflow_OtpVerificationAndGpsBroadcast_WorksCorrectly()
    {
        var manager = await CreateAuthorizedClientAsync(Roles.Manager);
        var cashier = await CreateAuthorizedClientAsync(Roles.Cashier);
        var driver = await CreateAuthorizedClientAsync(Roles.Driver);

        var zoneResp = await manager.PostAsJsonAsync("/api/delivery/zones",
            new UpsertDeliveryZoneRequest("Bonapriso Test", "Rue Koloko", 1500m, "09:00-12:00", 3, true));
        zoneResp.EnsureSuccessStatusCode();
        var zone = await zoneResp.Content.ReadFromJsonAsync<DeliveryZoneDto>();

        var custResp = await cashier.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            CustomerType.Individual, "Client Livraison", "+237677001122", null, null, "Bonapriso", null));
        var cust = await custResp.Content.ReadFromJsonAsync<CustomerDto>();

        var orderResp = await cashier.PostAsJsonAsync("/api/orders", new CreateOrderRequest(
            cust!.Id, DeliveryOption.HomeDelivery, zone!.Id, "Rue Koloko", false, null, 2500m, null, true, null,
            [new CreateOrderItemRequest("SHIRT", "WASH_IRON", false, false, null, null, null, null, null)]));
        var order = await orderResp.Content.ReadFromJsonAsync<OrderDto>();

        var missions = await (await manager.GetAsync("/api/delivery/missions")).Content.ReadFromJsonAsync<List<DeliveryOrderDto>>();
        var mission = missions!.First(m => m.OrderId == order!.Id);

        // Assign driver
        var driverId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var assignResp = await cashier.PostAsJsonAsync($"/api/delivery/missions/{mission.Id}/assign",
            new AssignDeliveryRequest(driverId, "Junior Tchinda"));
        assignResp.EnsureSuccessStatusCode();

        // Broadcast GPS
        var gpsResp = await driver.PostAsJsonAsync("/api/delivery/location",
            new BroadcastLocationRequest(mission.Id, 4.031, 9.695, 10));
        gpsResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Wrong OTP fails (RG-09)
        var badOtp = await driver.PostAsJsonAsync($"/api/delivery/missions/{mission.Id}/complete",
            new CompleteDeliveryRequest("0000", null, null));
        badOtp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Valid OTP completes delivery
        var goodOtp = await driver.PostAsJsonAsync($"/api/delivery/missions/{mission.Id}/complete",
            new CompleteDeliveryRequest(mission.OtpCodeForStaffPreview, null, null));
        goodOtp.StatusCode.Should().Be(HttpStatusCode.OK);
        var completed = await goodOtp.Content.ReadFromJsonAsync<DeliveryOrderDto>();
        completed!.Status.Should().Be(DeliveryStatus.Delivered);
    }
}

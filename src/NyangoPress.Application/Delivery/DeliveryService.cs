using FluentValidation;
using NyangoPress.Application.Common;
using NyangoPress.Application.Customers;
using NyangoPress.Domain.Delivery;
using NyangoPress.Domain.Orders;
using NyangoPress.Shared.Common;
using NyangoPress.Shared.Delivery;
using NyangoPress.Shared.Orders;
using NyangoPress.Shared.Tracking;
using DomainDeliveryMissionType = NyangoPress.Domain.Delivery.DeliveryMissionType;
using DomainDeliveryStatus = NyangoPress.Domain.Delivery.DeliveryStatus;
using DomainItemStatus = NyangoPress.Domain.Orders.ItemStatus;
using DomainPickupRequestStatus = NyangoPress.Domain.Delivery.PickupRequestStatus;
using SharedDeliveryStatus = NyangoPress.Shared.Delivery.DeliveryStatus;
using SharedPickupRequestStatus = NyangoPress.Shared.Delivery.PickupRequestStatus;

namespace NyangoPress.Application.Delivery;

public sealed class DeliveryService(
    IDeliveryStore store,
    ICustomerStore customerStore,
    IFileStorage fileStorage,
    ISequenceGenerator sequenceGenerator,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    IDomainEventDispatcher eventDispatcher,
    INotificationService notificationService,
    IRealtimeNotifier realtimeNotifier,
    ICurrentUser currentUser,
    IValidator<UpsertDeliveryZoneRequest> zoneValidator,
    IValidator<BroadcastLocationRequest> locationValidator) : IDeliveryService
{
    public async Task<IReadOnlyList<DeliveryZoneDto>> GetZonesAsync(bool activeOnly = false, CancellationToken ct = default)
        => (await store.GetZonesAsync(activeOnly, ct)).Select(MapZone).ToList();

    public async Task<Result<DeliveryZoneDto>> CreateZoneAsync(UpsertDeliveryZoneRequest request, CancellationToken ct = default)
    {
        var validation = await zoneValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Failure<DeliveryZoneDto>("Delivery.ZoneValidationFailed", validation.Errors[0].ErrorMessage);

        var zone = new DeliveryZone
        {
            BranchId = currentUser.BranchId,
            Name = request.Name.Trim(),
            Districts = request.Districts.Trim(),
            FeeFcfa = request.FeeFcfa,
            TimeSlots = request.TimeSlots.Trim(),
            MinimumDelayHours = request.MinimumDelayHours,
            IsActive = request.IsActive,
            CenterLatitude = request.CenterLatitude,
            CenterLongitude = request.CenterLongitude
        };

        await store.AddZoneAsync(zone, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await auditService.LogAsync("DeliveryZoneCreated", nameof(DeliveryZone), zone.Id, null, request, ct);
        return Result.Success(MapZone(zone));
    }

    public async Task<Result<DeliveryZoneDto>> UpdateZoneAsync(Guid id, UpsertDeliveryZoneRequest request, CancellationToken ct = default)
    {
        var validation = await zoneValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Failure<DeliveryZoneDto>("Delivery.ZoneValidationFailed", validation.Errors[0].ErrorMessage);

        var zone = await store.GetZoneByIdAsync(id, ct);
        if (zone is null)
            return Result.Failure<DeliveryZoneDto>("Delivery.ZoneNotFound", "Zone de livraison introuvable.");

        zone.Name = request.Name.Trim();
        zone.Districts = request.Districts.Trim();
        zone.FeeFcfa = request.FeeFcfa;
        zone.TimeSlots = request.TimeSlots.Trim();
        zone.MinimumDelayHours = request.MinimumDelayHours;
        zone.IsActive = request.IsActive;
        zone.CenterLatitude = request.CenterLatitude;
        zone.CenterLongitude = request.CenterLongitude;

        await unitOfWork.SaveChangesAsync(ct);
        await auditService.LogAsync("DeliveryZoneUpdated", nameof(DeliveryZone), zone.Id, null, request, ct);
        return Result.Success(MapZone(zone));
    }

    public async Task<IReadOnlyList<DeliveryOrderDto>> GetDeliveriesAsync(SharedDeliveryStatus? status = null, Guid? driverId = null, CancellationToken ct = default)
    {
        var list = await store.GetDeliveriesAsync(status, driverId, ct);
        var result = new List<DeliveryOrderDto>(list.Count);
        foreach (var d in list)
        {
            DriverLocationDto? locDto = null;
            if (d.DriverId.HasValue)
            {
                var loc = await store.GetLatestDriverLocationAsync(d.DriverId.Value, ct);
                if (loc is not null) locDto = MapLocation(loc);
            }
            result.Add(MapDelivery(d, locDto, includeOtpPreview: true));
        }
        return result;
    }

    public async Task<Result<DeliveryOrderDto>> AssignDriverAsync(Guid deliveryId, AssignDeliveryRequest request, CancellationToken ct = default)
    {
        var delivery = await store.GetDeliveryByIdAsync(deliveryId, ct);
        if (delivery is null)
            return Result.Failure<DeliveryOrderDto>("Delivery.NotFound", "Mission de livraison introuvable.");

        delivery.DriverId = request.DriverId;
        delivery.DriverName = request.DriverName.Trim();
        delivery.TourId = request.TourId;
        if (!string.IsNullOrWhiteSpace(request.TimeSlot))
            delivery.TimeSlot = request.TimeSlot.Trim();
        delivery.Status = DomainDeliveryStatus.Assigned;
        delivery.AssignedAt = DateTimeOffset.UtcNow;

        await unitOfWork.SaveChangesAsync(ct);
        await NotifyDeliveryAsync(delivery, "delivery.assigned", ct);

        return Result.Success(MapDelivery(delivery, null, includeOtpPreview: true));
    }

    public async Task<Result<DeliveryOrderDto>> UpdateStatusAsync(Guid deliveryId, UpdateDeliveryStatusRequest request, CancellationToken ct = default)
    {
        var delivery = await store.GetDeliveryByIdAsync(deliveryId, ct);
        if (delivery is null)
            return Result.Failure<DeliveryOrderDto>("Delivery.NotFound", "Mission introuvable.");

        if (currentUser.IsInRole(Roles.Driver) && delivery.DriverId.HasValue && currentUser.UserId.HasValue && delivery.DriverId.Value != currentUser.UserId.Value)
        {
            return Result.Failure<DeliveryOrderDto>("Delivery.Forbidden", "Vous ne pouvez modifier que vos propres missions.");
        }

        var now = DateTimeOffset.UtcNow;
        delivery.Status = request.Status.ToDomain();
        switch (delivery.Status)
        {
            case DomainDeliveryStatus.EnRoute:
                delivery.StartedAt ??= now;
                await NotifyDeliveryAsync(delivery, "delivery.enroute", ct);
                break;
            case DomainDeliveryStatus.Arrived:
            case DomainDeliveryStatus.NearDestination:
                delivery.ArrivedAt ??= now;
                await NotifyDeliveryAsync(delivery, "delivery.near", ct);
                break;
            case DomainDeliveryStatus.Failed:
                delivery.FailureReason = request.Notes?.Trim() ?? "Client absent";
                break;
        }

        await unitOfWork.SaveChangesAsync(ct);
        var ticket = delivery.Order?.TicketNumber ?? string.Empty;
        await realtimeNotifier.NotifyDeliveryStatusChangedAsync(delivery.Id, ticket, delivery.Status.ToString(), ct);
        return Result.Success(MapDelivery(delivery, null, includeOtpPreview: !currentUser.IsInRole(Roles.Driver)));
    }

    public async Task<Result<DeliveryOrderDto>> CompleteDeliveryAsync(Guid deliveryId, CompleteDeliveryRequest request, CancellationToken ct = default)
    {
        var delivery = await store.GetDeliveryByIdAsync(deliveryId, ct);
        if (delivery is null)
            return Result.Failure<DeliveryOrderDto>("Delivery.NotFound", "Mission introuvable.");

        var hasPhoto = !string.IsNullOrWhiteSpace(request.ProofPhotoDataUrl);
        var hasSig = !string.IsNullOrWhiteSpace(request.ProofSignatureDataUrl);
        var hasOtp = !string.IsNullOrWhiteSpace(request.OtpCode);

        if (!hasOtp && !hasPhoto && !hasSig)
            return Result.Failure<DeliveryOrderDto>("Delivery.ProofRequired", "Une preuve est requise : code à 4 chiffres, photo ou signature.");

        // RG-09: when OTP is supplied, verify it strictly matches
        if (hasOtp && !string.Equals(request.OtpCode!.Trim(), delivery.OtpCode, StringComparison.Ordinal))
            return Result.Failure<DeliveryOrderDto>("Delivery.InvalidOtp", "Code de remise à 4 chiffres incorrect (RG-09).");

        var now = DateTimeOffset.UtcNow;
        if (hasPhoto)
        {
            var photo = await fileStorage.SaveBase64ImageAsync(request.ProofPhotoDataUrl!, "deliveries", delivery.Id.ToString("N")[..8], ct);
            delivery.ProofPhotoPath = photo.StoragePath;
            delivery.ProofPhotoUrl = photo.PublicUrl;
        }
        if (hasSig)
        {
            var sig = await fileStorage.SaveBase64ImageAsync(request.ProofSignatureDataUrl!, "deliveries", $"sig-{delivery.Id:N}"[..12], ct);
            delivery.ProofSignaturePath = sig.StoragePath;
            delivery.ProofSignatureUrl = sig.PublicUrl;
        }

        delivery.Status = DomainDeliveryStatus.Delivered;
        delivery.CompletedAt = now;

        if (delivery.Order is not null && delivery.MissionType == DomainDeliveryMissionType.Delivery)
        {
            foreach (var item in delivery.Order.Items.Where(i => i.Status == DomainItemStatus.Ready))
            {
                item.Status = DomainItemStatus.Delivered;
                item.StatusHistory.Add(new ItemStatusHistory
                {
                    BranchId = item.BranchId,
                    OrderItemId = item.Id,
                    FromStatus = DomainItemStatus.Ready,
                    ToStatus = DomainItemStatus.Delivered,
                    ChangedBy = currentUser.DisplayName,
                    ChangedByUserId = currentUser.UserId,
                    ChangedAt = now,
                    Notes = "Livré à domicile avec preuve de remise"
                });
            }
            delivery.Order.RefreshStatus(now);
        }

        await unitOfWork.SaveChangesAsync(ct);
        await NotifyDeliveryAsync(delivery, "delivery.delivered", ct);
        await auditService.LogAsync("DeliveryCompleted", nameof(DeliveryOrder), delivery.Id, null, new { delivery.OrderId, hasOtp, hasPhoto, hasSig }, ct);
        return Result.Success(MapDelivery(delivery, null, includeOtpPreview: false));
    }

    public async Task<IReadOnlyList<DeliveryTourDto>> GetToursAsync(DateOnly? date = null, Guid? driverId = null, CancellationToken ct = default)
        => (await store.GetToursAsync(date, driverId, ct)).Select(MapTour).ToList();

    public async Task<Result<DeliveryTourDto>> CreateTourAsync(CreateTourRequest request, CancellationToken ct = default)
    {
        if (request.DeliveryOrderIds.Count == 0)
            return Result.Failure<DeliveryTourDto>("Delivery.TourEmpty", "Sélectionnez au moins une livraison pour créer la tournée.");

        var tour = new DeliveryTour
        {
            BranchId = currentUser.BranchId,
            TourCode = await sequenceGenerator.NextTourCodeAsync(ct),
            DriverId = request.DriverId,
            DriverName = request.DriverName.Trim(),
            TourDate = request.TourDate
        };

        var seq = 1;
        foreach (var deliveryId in request.DeliveryOrderIds)
        {
            var delivery = await store.GetDeliveryByIdAsync(deliveryId, ct);
            if (delivery is null) continue;
            delivery.TourId = tour.Id;
            delivery.DriverId = tour.DriverId;
            delivery.DriverName = tour.DriverName;
            delivery.StopSequence = seq++;
            if (delivery.Status == DomainDeliveryStatus.Pending)
            {
                delivery.Status = DomainDeliveryStatus.Assigned;
                delivery.AssignedAt = DateTimeOffset.UtcNow;
            }
            tour.Stops.Add(delivery);
        }

        await store.AddTourAsync(tour, ct);
        await unitOfWork.SaveChangesAsync(ct);

        foreach (var stop in tour.Stops)
        {
            await NotifyDeliveryAsync(stop, "delivery.assigned", ct);
        }

        return Result.Success(MapTour(tour));
    }

    public async Task<Result<DeliveryTourDto>> ReorderTourStopsAsync(Guid tourId, ReorderTourStopsRequest request, CancellationToken ct = default)
    {
        var tour = await store.GetTourByIdAsync(tourId, ct);
        if (tour is null)
            return Result.Failure<DeliveryTourDto>("Delivery.TourNotFound", "Tournée introuvable.");

        for (var idx = 0; idx < request.OrderedDeliveryIds.Count; idx++)
        {
            var stop = tour.Stops.FirstOrDefault(s => s.Id == request.OrderedDeliveryIds[idx]);
            if (stop is not null) stop.StopSequence = idx + 1;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(MapTour(tour));
    }

    public async Task<Result<DeliveryTourDto>> StartTourAsync(Guid tourId, CancellationToken ct = default)
    {
        var tour = await store.GetTourByIdAsync(tourId, ct);
        if (tour is null)
            return Result.Failure<DeliveryTourDto>("Delivery.TourNotFound", "Tournée introuvable.");

        var now = DateTimeOffset.UtcNow;
        tour.IsStarted = true;
        tour.StartedAt ??= now;

        foreach (var stop in tour.Stops.Where(s => s.Status is DomainDeliveryStatus.Pending or DomainDeliveryStatus.Assigned))
        {
            stop.Status = DomainDeliveryStatus.EnRoute;
            stop.StartedAt ??= now;
            await NotifyDeliveryAsync(stop, "delivery.enroute", ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(MapTour(tour));
    }

    public async Task<Result<DriverLocationDto>> BroadcastLocationAsync(BroadcastLocationRequest request, CancellationToken ct = default)
    {
        var validation = await locationValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Failure<DriverLocationDto>("Delivery.LocationInvalid", validation.Errors[0].ErrorMessage);

        var driverId = currentUser.UserId ?? Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var loc = new DriverLocation
        {
            BranchId = currentUser.BranchId,
            DriverId = driverId,
            DriverName = currentUser.DisplayName,
            DeliveryOrderId = request.DeliveryOrderId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            AccuracyMeters = request.AccuracyMeters,
            RecordedAt = now
        };

        await store.AddDriverLocationAsync(loc, ct);
        await unitOfWork.SaveChangesAsync(ct);

        string? ticket = null;
        if (request.DeliveryOrderId.HasValue)
        {
            var delivery = await store.GetDeliveryByIdAsync(request.DeliveryOrderId.Value, ct);
            ticket = delivery?.Order?.TicketNumber;
        }

        await realtimeNotifier.NotifyDriverLocationAsync(driverId, currentUser.DisplayName, request.DeliveryOrderId, ticket, request.Latitude, request.Longitude, now, ct);
        return Result.Success(MapLocation(loc));
    }

    public async Task<IReadOnlyList<PickupRequestDto>> GetPickupRequestsAsync(SharedPickupRequestStatus? status = null, CancellationToken ct = default)
        => (await store.GetPickupRequestsAsync(status, ct)).Select(MapPickupRequest).ToList();

    public async Task<Result<PickupRequestDto>> ReviewPickupRequestAsync(Guid id, ReviewPickupRequest request, CancellationToken ct = default)
    {
        var pr = await store.GetPickupRequestByIdAsync(id, ct);
        if (pr is null)
            return Result.Failure<PickupRequestDto>("Delivery.PickupRequestNotFound", "Demande de collecte introuvable.");

        pr.Status = request.Approve ? DomainPickupRequestStatus.Approved : DomainPickupRequestStatus.Rejected;
        pr.ReviewNotes = request.ReviewNotes?.Trim();

        await unitOfWork.SaveChangesAsync(ct);
        await auditService.LogAsync("PickupRequestReviewed", nameof(PickupRequest), pr.Id, null, request, ct);
        return Result.Success(MapPickupRequest(pr));
    }

    private async Task NotifyDeliveryAsync(DeliveryOrder delivery, string templateKey, CancellationToken ct)
    {
        var ticket = delivery.Order?.TicketNumber ?? string.Empty;
        await eventDispatcher.DispatchAsync(
            new DeliveryStatusChangedEvent(delivery.Id, delivery.OrderId, delivery.CustomerId, ticket, delivery.Status, delivery.OtpCode),
            ct);

        var customer = await customerStore.GetByIdAsync(delivery.CustomerId, includeDetails: false, ct);
        if (customer is not null)
        {
            await notificationService.EnqueueAsync(
                new NotificationRequest(
                    templateKey,
                    customer.Id,
                    null,
                    customer.Email,
                    customer.WhatsAppNumber ?? customer.Phone,
                    new Dictionary<string, string>
                    {
                        ["customer_name"] = customer.FullName,
                        ["ticket"] = ticket,
                        ["otp_code"] = delivery.OtpCode,
                        ["driver_name"] = delivery.DriverName ?? string.Empty
                    }),
                ct);
        }

        await realtimeNotifier.NotifyDeliveryStatusChangedAsync(delivery.Id, ticket, delivery.Status.ToString(), ct);
    }

    internal static DeliveryZoneDto MapZone(DeliveryZone z) => new(
        z.Id, z.Name, z.Districts, z.FeeFcfa, z.TimeSlots, z.MinimumDelayHours,
        z.IsActive, z.CenterLatitude, z.CenterLongitude, z.RowVersion);

    internal static DriverLocationDto MapLocation(DriverLocation l) => new(
        l.DriverId, l.DriverName, l.Latitude, l.Longitude, l.AccuracyMeters, l.RecordedAt);

    internal static DeliveryOrderDto MapDelivery(DeliveryOrder d, DriverLocationDto? loc, bool includeOtpPreview) => new(
        d.Id,
        d.OrderId,
        d.Order?.TicketNumber ?? string.Empty,
        d.CustomerId,
        d.Order?.Customer?.FullName ?? string.Empty,
        d.Order?.Customer?.Phone ?? string.Empty,
        d.ZoneId,
        d.Zone?.Name ?? string.Empty,
        d.DeliveryFeeFcfa,
        d.MissionType.ToShared(),
        d.Status.ToShared(),
        d.TourId,
        d.StopSequence,
        d.DriverId,
        d.DriverName,
        d.Address,
        d.DestinationLatitude,
        d.DestinationLongitude,
        d.TimeSlot,
        d.ScheduledAt,
        d.AssignedAt,
        d.StartedAt,
        d.ArrivedAt,
        d.CompletedAt,
        includeOtpPreview ? d.OtpCode : null,
        d.ProofPhotoUrl,
        d.ProofSignatureUrl,
        d.FailureReason,
        loc,
        d.RowVersion);

    private static DeliveryTourDto MapTour(DeliveryTour t) => new(
        t.Id,
        t.TourCode,
        t.DriverId,
        t.DriverName,
        t.TourDate,
        t.IsStarted,
        t.IsCompleted,
        t.StartedAt,
        t.CompletedAt,
        t.Stops.OrderBy(s => s.StopSequence).Select(s => MapDelivery(s, null, includeOtpPreview: true)).ToList());

    internal static PickupRequestDto MapPickupRequest(PickupRequest p) => new(
        p.Id,
        p.ReferenceCode,
        p.CustomerName,
        p.CustomerPhone,
        p.CustomerEmail,
        p.ZoneId,
        p.Zone?.Name ?? string.Empty,
        p.Address,
        p.PreferredTimeSlot,
        p.EstimatedItemsCount,
        p.Notes,
        p.Status.ToShared(),
        p.ReviewNotes,
        p.LinkedOrderId,
        p.CreatedAt);
}

public sealed class PublicTrackingService(
    IDeliveryStore store,
    ISequenceGenerator sequenceGenerator,
    IUnitOfWork unitOfWork,
    IValidator<CreatePickupRequest> pickupValidator) : IPublicTrackingService
{
    public async Task<Result<PublicOrderTrackingDto>> TrackOrderAsync(
        string ticketNumber,
        string trackingCode,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ticketNumber) || string.IsNullOrWhiteSpace(trackingCode))
            return Result.Failure<PublicOrderTrackingDto>("Tracking.InvalidCredentials", "Numéro de ticket et code de suivi requis.");

        var order = await store.GetOrderByTicketAndCodeAsync(ticketNumber.Trim().ToUpperInvariant(), trackingCode.Trim().ToUpperInvariant(), ct);
        if (order is null)
            return Result.Failure<PublicOrderTrackingDto>("Tracking.NotFound", "Aucune commande ne correspond à ce ticket et ce code.");

        PublicDeliveryTrackingDto? deliveryTracking = null;
        if (order.DeliveryOrder is not null)
        {
            DriverLocation? loc = null;
            if (order.DeliveryOrder.DriverId.HasValue &&
                order.DeliveryOrder.Status is DomainDeliveryStatus.EnRoute or DomainDeliveryStatus.Arrived or DomainDeliveryStatus.NearDestination)
            {
                loc = await store.GetLatestDriverLocationAsync(order.DeliveryOrder.DriverId.Value, ct);
            }

            var driverFirst = string.IsNullOrWhiteSpace(order.DeliveryOrder.DriverName)
                ? null
                : order.DeliveryOrder.DriverName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

            deliveryTracking = new PublicDeliveryTrackingDto(
                order.DeliveryOrder.Status.ToShared(),
                order.DeliveryOrder.Zone?.Name ?? string.Empty,
                order.DeliveryOrder.TimeSlot,
                driverFirst,
                loc?.Latitude,
                loc?.Longitude,
                order.DeliveryOrder.DestinationLatitude,
                order.DeliveryOrder.DestinationLongitude,
                loc?.RecordedAt);
        }

        // Strict PII minimization: mask customer full name (POR-01)
        var maskedName = MaskCustomerName(order.Customer?.FullName);

        return Result.Success(new PublicOrderTrackingDto(
            order.TicketNumber,
            maskedName,
            order.Status.ToShared(),
            order.DeliveryOption.ToShared(),
            order.IsExpress,
            order.CreatedAt,
            order.PromisedAt,
            order.EstimatedReadyAt,
            order.Items.Count,
            order.Items.Count(i => i.Status == DomainItemStatus.Ready),
            order.TotalFcfa + order.StorageFeeFcfa,
            order.PaidFcfa,
            order.BalanceFcfa,
            order.Items.Select(i => new PublicTrackingItemDto(
                i.GarmentName,
                i.ServiceName,
                i.Status.ToShared(),
                i.IsExpress,
                i.StorageLocation?.Code,
                i.StatusHistory.OrderBy(h => h.ChangedAt)
                    .Select(h => new PublicTimelineStepDto(h.ToStatus.ToShared(), h.ChangedAt))
                    .ToList())).ToList(),
            deliveryTracking));
    }

    public async Task<Result<PublicPickupRequestResultDto>> SubmitPickupRequestAsync(CreatePickupRequest request, CancellationToken ct = default)
    {
        var validation = await pickupValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Failure<PublicPickupRequestResultDto>("Tracking.ValidationFailed", validation.Errors[0].ErrorMessage);

        var zone = await store.GetZoneByIdAsync(request.ZoneId, ct);
        if (zone is null || !zone.IsActive)
            return Result.Failure<PublicPickupRequestResultDto>("Delivery.ZoneInvalid", "Zone de collecte invalide.");

        var pr = new PickupRequest
        {
            ReferenceCode = await sequenceGenerator.NextPickupReferenceAsync(ct),
            CustomerName = request.CustomerName.Trim(),
            CustomerPhone = request.CustomerPhone.Trim(),
            CustomerEmail = request.CustomerEmail?.Trim(),
            ZoneId = zone.Id,
            Zone = zone,
            Address = request.Address.Trim(),
            PreferredTimeSlot = request.PreferredTimeSlot.Trim(),
            EstimatedItemsCount = request.EstimatedItemsCount,
            Notes = request.Notes?.Trim(),
            Status = DomainPickupRequestStatus.Pending
        };

        await store.AddPickupRequestAsync(pr, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new PublicPickupRequestResultDto(
            pr.Id, pr.ReferenceCode, pr.Status.ToShared(), zone.Name, pr.PreferredTimeSlot, pr.CreatedAt));
    }

    public async Task<IReadOnlyList<DeliveryZoneDto>> GetPublicZonesAsync(CancellationToken ct = default)
        => (await store.GetZonesAsync(activeOnly: true, ct)).Select(DeliveryService.MapZone).ToList();

    private static string MaskCustomerName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "Client";
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
            return parts[0].Length <= 2 ? parts[0] : $"{parts[0][..2]}***";
        return $"{parts[0]} {parts[^1][0]}.";
    }
}

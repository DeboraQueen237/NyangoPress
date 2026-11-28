using System.Security.Cryptography;
using FluentValidation;
using NyangoPress.Application.Common;
using NyangoPress.Application.Customers;
using NyangoPress.Domain.Delivery;
using NyangoPress.Domain.Orders;
using NyangoPress.Shared.Common;
using NyangoPress.Shared.Delivery;
using NyangoPress.Shared.Orders;
using DomainItemStatus = NyangoPress.Domain.Orders.ItemStatus;
using DomainOrderStatus = NyangoPress.Domain.Orders.OrderStatus;
using DomainDeliveryOption = NyangoPress.Domain.Orders.DeliveryOption;
using DomainTreatmentSubStep = NyangoPress.Domain.Orders.TreatmentSubStep;
using DomainClaimStatus = NyangoPress.Domain.Orders.ClaimStatus;
using DomainDeliveryMissionType = NyangoPress.Domain.Delivery.DeliveryMissionType;
using DomainDeliveryStatus = NyangoPress.Domain.Delivery.DeliveryStatus;
using SharedDeliveryOption = NyangoPress.Shared.Orders.DeliveryOption;
using SharedItemStatus = NyangoPress.Shared.Orders.ItemStatus;
using SharedTreatmentSubStep = NyangoPress.Shared.Orders.TreatmentSubStep;

namespace NyangoPress.Application.Orders;

public sealed class OrderService(
    IOrderStore orderStore,
    ICustomerStore customerStore,
    IPricingService pricingService,
    IQrCodeService qrCodeService,
    IFileStorage fileStorage,
    ISequenceGenerator sequenceGenerator,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    IDomainEventDispatcher eventDispatcher,
    INotificationService notificationService,
    IRealtimeNotifier realtimeNotifier,
    ICurrentUser currentUser,
    IValidator<CreateOrderRequest> createValidator,
    IValidator<PickupOrderRequest> pickupValidator,
    IValidator<CancelOrderRequest> cancelValidator,
    IValidator<CreateClaimRequest> claimValidator,
    IValidator<ResolveClaimRequest> resolveClaimValidator,
    IValidator<CreateStorageLocationRequest> storageValidator) : IOrderService
{
    private const string DefaultTermsSummary =
        "Frais de garde applicables après 7 jours de disponibilité. Articles non réclamés après 60 jours éligibles aux enchères selon les CGV NyangoPress.";

    public Task<PagedResult<OrderSummaryDto>> SearchAsync(OrderQueryParams query, CancellationToken ct = default)
        => orderStore.SearchAsync(query, ct);

    public async Task<Result<OrderDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var order = await orderStore.GetByIdAsync(id, includeDetails: true, ct);
        return order is null
            ? Result.Failure<OrderDto>("Order.NotFound", "Commande introuvable.")
            : Result.Success(MapToDto(order));
    }

    public async Task<Result<OrderDto>> GetByTicketAsync(string ticketNumber, CancellationToken ct = default)
    {
        var order = await orderStore.GetByTicketAsync(ticketNumber.Trim().ToUpperInvariant(), includeDetails: true, ct);
        return order is null
            ? Result.Failure<OrderDto>("Order.NotFound", "Ticket introuvable.")
            : Result.Success(MapToDto(order));
    }

    public async Task<Result<OrderPricePreviewDto>> PreviewPriceAsync(OrderPricePreviewRequest request, CancellationToken ct = default)
    {
        var customer = await customerStore.GetByIdAsync(request.CustomerId, includeDetails: true, ct);
        if (customer is null)
            return Result.Failure<OrderPricePreviewDto>("Customer.NotFound", "Client introuvable.");

        decimal deliveryFee = 0m;
        if (request.DeliveryOption != SharedDeliveryOption.StorePickup && request.DeliveryZoneId.HasValue)
        {
            var zone = await orderStore.GetDeliveryZoneByIdAsync(request.DeliveryZoneId.Value, ct);
            if (zone is not null) deliveryFee = zone.FeeFcfa;
        }

        var contractDiscount = customer.CompanyContract is { IsActive: true }
            ? customer.CompanyContract.NegotiatedDiscountPercent
            : 0m;

        var draft = new OrderDraft(
            request.CustomerId,
            request.DeliveryOption,
            deliveryFee,
            contractDiscount,
            request.IsExpress,
            request.PromoCode,
            request.Items.Select(i => new OrderDraftLine(
                i.GarmentCode,
                i.ServiceCode,
                i.IsExpress || request.IsExpress,
                i.IsBulky)).ToList());

        var pricing = await pricingService.CalculateAsync(draft, ct);
        var estimate = await EstimatePickupDateAsync(
            new EstimatePickupDateRequest(
                request.Items.Select(i => i.ServiceCode).ToList(),
                request.IsExpress || request.Items.Any(i => i.IsExpress),
                request.DeliveryOption),
            ct);

        return Result.Success(new OrderPricePreviewDto(
            pricing.Lines.Select(l => new OrderPriceLineDto(
                l.GarmentCode,
                l.GarmentName,
                l.ServiceCode,
                l.ServiceName,
                l.BasePriceFcfa,
                l.SurchargeFcfa,
                l.DiscountFcfa,
                l.FinalPriceFcfa)).ToList(),
            pricing.SubtotalFcfa,
            pricing.DeliveryFeeFcfa,
            pricing.DiscountFcfa,
            pricing.TotalFcfa,
            estimate.EstimatedReadyAt));
    }

    public async Task<EstimatePickupDateResponse> EstimatePickupDateAsync(EstimatePickupDateRequest request, CancellationToken ct = default)
    {
        var activeItems = await orderStore.CountActiveWorkshopItemsAsync(ct);
        var services = pricingService.GetServices().ToDictionary(s => s.Code, StringComparer.OrdinalIgnoreCase);

        var maxServiceHours = request.ServiceCodes.Count == 0
            ? 24
            : request.ServiceCodes.Max(code => services.TryGetValue(code, out var svc) ? svc.StandardHours : 24);

        // Add intelligent load factor: +4h per 25 active items in workshop
        var loadExtraHours = (activeItems / 25) * 4;
        var baseHours = maxServiceHours + loadExtraHours;

        if (request.IsExpress)
        {
            baseHours = Math.Max(4, (int)Math.Ceiling(baseHours * 0.45));
        }

        if (request.DeliveryOption != SharedDeliveryOption.StorePickup)
        {
            baseHours += 4;
        }

        var estimated = DateTimeOffset.UtcNow.AddHours(baseHours);
        var explanation = request.IsExpress
            ? "Orders.Estimate.ExpressWithWorkshopLoad"
            : loadExtraHours > 0
                ? "Orders.Estimate.StandardHighLoad"
                : "Orders.Estimate.StandardNormalLoad";

        return new EstimatePickupDateResponse(estimated, activeItems, baseHours, explanation);
    }

    public async Task<Result<OrderDto>> CreateAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Failure<OrderDto>("Order.ValidationFailed", validation.Errors[0].ErrorMessage);

        var customer = await customerStore.GetByIdAsync(request.CustomerId, includeDetails: true, ct);
        if (customer is null)
            return Result.Failure<OrderDto>("Customer.NotFound", "Client introuvable.");

        DeliveryZone? zone = null;
        if (request.DeliveryOption != SharedDeliveryOption.StorePickup)
        {
            if (!request.DeliveryZoneId.HasValue)
                return Result.Failure<OrderDto>("Order.DeliveryZoneRequired", "La zone de livraison est obligatoire.");

            zone = await orderStore.GetDeliveryZoneByIdAsync(request.DeliveryZoneId.Value, ct);
            if (zone is null || !zone.IsActive)
                return Result.Failure<OrderDto>("Order.DeliveryZoneInvalid", "Zone de livraison invalide ou inactive.");
        }

        var deliveryFee = zone?.FeeFcfa ?? 0m;
        var contractDiscount = customer.CompanyContract is { IsActive: true }
            ? customer.CompanyContract.NegotiatedDiscountPercent
            : 0m;

        var pricing = await pricingService.CalculateAsync(
            new OrderDraft(
                customer.Id,
                request.DeliveryOption,
                deliveryFee,
                contractDiscount,
                request.IsExpress,
                request.PromoCode,
                request.Items.Select(i => new OrderDraftLine(
                    i.GarmentCode,
                    i.ServiceCode,
                    i.IsExpress || request.IsExpress,
                    i.IsBulky)).ToList()),
            ct);

        var ticketNumber = await sequenceGenerator.NextTicketNumberAsync(ct);
        var trackingCode = GenerateSecretCode(6);
        var now = DateTimeOffset.UtcNow;

        var estimate = await EstimatePickupDateAsync(
            new EstimatePickupDateRequest(
                request.Items.Select(i => i.ServiceCode).ToList(),
                request.IsExpress || request.Items.Any(i => i.IsExpress),
                request.DeliveryOption),
            ct);

        var promisedAt = request.PromisedAt ?? estimate.EstimatedReadyAt;
        var initialDeposit = Math.Min(request.InitialDepositFcfa, pricing.TotalFcfa);

        var order = new Order
        {
            BranchId = currentUser.BranchId,
            TicketNumber = ticketNumber,
            TrackingCode = trackingCode,
            CustomerId = customer.Id,
            Customer = customer,
            Status = DomainOrderStatus.Received,
            DeliveryOption = request.DeliveryOption.ToDomain(),
            DeliveryZoneId = zone?.Id,
            DeliveryAddress = request.DeliveryOption == SharedDeliveryOption.StorePickup
                ? null
                : (string.IsNullOrWhiteSpace(request.DeliveryAddress) ? customer.Address : request.DeliveryAddress.Trim()),
            IsExpress = request.IsExpress || request.Items.Any(i => i.IsExpress),
            AcceptedTermsAndConditions = request.AcceptedTermsAndConditions,
            TermsAcceptedAt = now,
            TermsSummary = DefaultTermsSummary,
            SubtotalFcfa = pricing.SubtotalFcfa,
            DeliveryFeeFcfa = pricing.DeliveryFeeFcfa,
            DiscountFcfa = pricing.DiscountFcfa,
            TotalFcfa = pricing.TotalFcfa,
            PaidFcfa = initialDeposit,
            PromisedAt = promisedAt,
            EstimatedReadyAt = estimate.EstimatedReadyAt,
            PromoCode = request.PromoCode?.Trim(),
            Notes = request.Notes?.Trim()
        };

        for (var idx = 0; idx < request.Items.Count; idx++)
        {
            var itemReq = request.Items[idx];
            var linePrice = pricing.Lines[idx];
            var qrCode = $"NP-{ticketNumber.Replace("NP-", string.Empty)}-{idx + 1:D2}-{GenerateSecretCode(4)}";

            var item = new OrderItem
            {
                BranchId = currentUser.BranchId,
                OrderId = order.Id,
                QrCode = qrCode,
                GarmentCode = linePrice.GarmentCode,
                GarmentName = linePrice.GarmentName,
                ServiceCode = linePrice.ServiceCode,
                ServiceName = linePrice.ServiceName,
                Status = DomainItemStatus.Received,
                TreatmentSubStep = DomainTreatmentSubStep.None,
                IsExpress = itemReq.IsExpress || request.IsExpress,
                IsBulky = itemReq.IsBulky,
                Color = itemReq.Color?.Trim(),
                Brand = itemReq.Brand?.Trim(),
                DefectNotes = itemReq.DefectNotes?.Trim(),
                BasePriceFcfa = linePrice.BasePriceFcfa,
                SurchargeFcfa = linePrice.SurchargeFcfa,
                DiscountFcfa = linePrice.DiscountFcfa,
                FinalPriceFcfa = linePrice.FinalPriceFcfa
            };
            item.SetDefectTags(itemReq.PredefinedDefects);

            item.StatusHistory.Add(new ItemStatusHistory
            {
                BranchId = currentUser.BranchId,
                OrderItemId = item.Id,
                FromStatus = null,
                ToStatus = DomainItemStatus.Received,
                SubStep = DomainTreatmentSubStep.None,
                ChangedBy = currentUser.DisplayName,
                ChangedByUserId = currentUser.UserId,
                ChangedAt = now,
                Notes = "Dépôt initial enregistré"
            });

            if (itemReq.PhotoDataUrls is { Count: > 0 })
            {
                foreach (var dataUrl in itemReq.PhotoDataUrls.Take(5))
                {
                    if (string.IsNullOrWhiteSpace(dataUrl)) continue;
                    var stored = await fileStorage.SaveBase64ImageAsync(dataUrl, "items", qrCode, ct);
                    item.Photos.Add(new ItemPhoto
                    {
                        BranchId = currentUser.BranchId,
                        OrderItemId = item.Id,
                        FileName = stored.FileName,
                        StoragePath = stored.StoragePath,
                        PublicUrl = stored.PublicUrl,
                        ContentType = stored.ContentType,
                        SizeBytes = stored.SizeBytes,
                        CapturedAt = now
                    });
                }
            }

            order.Items.Add(item);
        }

        await orderStore.AddOrderAsync(order, ct);

        if (zone is not null)
        {
            var delivery = new DeliveryOrder
            {
                BranchId = currentUser.BranchId,
                OrderId = order.Id,
                CustomerId = customer.Id,
                ZoneId = zone.Id,
                DeliveryFeeFcfa = zone.FeeFcfa,
                MissionType = request.DeliveryOption == SharedDeliveryOption.CollectAndDeliver
                    ? DomainDeliveryMissionType.Pickup
                    : DomainDeliveryMissionType.Delivery,
                Status = DomainDeliveryStatus.Pending,
                Address = order.DeliveryAddress ?? customer.Address ?? zone.Name,
                DestinationLatitude = zone.CenterLatitude,
                DestinationLongitude = zone.CenterLongitude,
                ScheduledAt = promisedAt,
                OtpCode = GenerateNumericOtp(4)
            };
            await orderStore.AddDeliveryOrderAsync(delivery, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogAsync(
            "OrderCreated",
            nameof(Order),
            order.Id,
            null,
            new { order.TicketNumber, order.CustomerId, order.TotalFcfa, ItemsCount = order.Items.Count },
            ct);

        await eventDispatcher.DispatchAsync(
            new OrderCreatedEvent(order.Id, order.TicketNumber, order.CustomerId, order.TotalFcfa, order.PromisedAt),
            ct);

        await notificationService.EnqueueAsync(
            new NotificationRequest(
                "order.received",
                customer.Id,
                null,
                customer.Email,
                customer.WhatsAppNumber ?? customer.Phone,
                new Dictionary<string, string>
                {
                    ["customer_name"] = customer.FullName,
                    ["ticket"] = order.TicketNumber,
                    ["tracking_code"] = order.TrackingCode,
                    ["amount"] = order.TotalFcfa.ToString("N0"),
                    ["ready_date"] = order.PromisedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                }),
            ct);

        await realtimeNotifier.NotifyOrderUpdatedAsync(order.Id, order.TicketNumber, ct);

        return Result.Success(MapToDto(order));
    }

    public async Task<Result<OrderItemDto>> UpdateInspectionAsync(
        Guid orderId,
        Guid itemId,
        UpdateOrderItemInspectionRequest request,
        CancellationToken ct = default)
    {
        var item = await orderStore.GetItemByIdAsync(itemId, ct);
        if (item is null || item.OrderId != orderId)
            return Result.Failure<OrderItemDto>("OrderItem.NotFound", "Article introuvable.");

        if (item.RowVersion != request.RowVersion)
            return Result.Failure<OrderItemDto>("OrderItem.ConcurrencyConflict", "Cet article a été modifié simultanément.");

        item.Color = request.Color?.Trim();
        item.Brand = request.Brand?.Trim();
        item.SetDefectTags(request.PredefinedDefects);
        item.DefectNotes = request.DefectNotes?.Trim();

        await unitOfWork.SaveChangesAsync(ct);
        await auditService.LogAsync("ItemInspectionUpdated", nameof(OrderItem), item.Id, null, request, ct);
        return Result.Success(MapItemToDto(item, includeQrSvg: true));
    }

    public async Task<Result<ItemPhotoDto>> AddItemPhotoAsync(
        Guid orderId,
        Guid itemId,
        AddItemPhotoRequest request,
        CancellationToken ct = default)
    {
        var item = await orderStore.GetItemByIdAsync(itemId, ct);
        if (item is null || item.OrderId != orderId)
            return Result.Failure<ItemPhotoDto>("OrderItem.NotFound", "Article introuvable.");

        if (string.IsNullOrWhiteSpace(request.Base64DataUrl))
            return Result.Failure<ItemPhotoDto>("OrderItem.InvalidPhoto", "Photo invalide.");

        var stored = await fileStorage.SaveBase64ImageAsync(request.Base64DataUrl, "items", item.QrCode, ct);
        var photo = new ItemPhoto
        {
            BranchId = item.BranchId,
            OrderItemId = item.Id,
            FileName = stored.FileName,
            StoragePath = stored.StoragePath,
            PublicUrl = stored.PublicUrl,
            ContentType = stored.ContentType,
            SizeBytes = stored.SizeBytes,
            Caption = request.Caption?.Trim(),
            CapturedAt = DateTimeOffset.UtcNow
        };
        item.Photos.Add(photo);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new ItemPhotoDto(photo.Id, photo.FileName, photo.PublicUrl, photo.Caption, photo.CapturedAt));
    }

    public Task<IReadOnlyList<ProductionItemCardDto>> GetProductionQueueAsync(
        SharedItemStatus? status = null,
        string? serviceCode = null,
        CancellationToken ct = default)
        => orderStore.GetProductionQueueAsync(status, serviceCode, ct);

    public async Task<Result<OrderItemDto>> AdvanceItemStatusAsync(
        Guid itemId,
        AdvanceItemStatusRequest request,
        CancellationToken ct = default)
    {
        var item = await orderStore.GetItemByIdAsync(itemId, ct);
        if (item is null || item.Order is null)
            return Result.Failure<OrderItemDto>("OrderItem.NotFound", "Article introuvable.");

        if (request.RowVersion.HasValue && item.RowVersion != request.RowVersion.Value)
            return Result.Failure<OrderItemDto>("OrderItem.ConcurrencyConflict", "Cet article vient d'être mis à jour par un autre opérateur.");

        return await TransitionItemAsync(item, request.TargetStatus.ToDomain(), request.SubStep.ToDomain(), request.StorageLocationId, request.Notes, ct);
    }

    public async Task<Result<QrScanResultDto>> ScanQrAsync(QrScanRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.QrCode))
            return Result.Failure<QrScanResultDto>("Qr.Empty", "Code QR requis.");

        var item = await orderStore.GetItemByQrAsync(request.QrCode.Trim(), ct);
        if (item is null || item.Order is null)
            return Result.Failure<QrScanResultDto>("Qr.NotFound", $"Aucun article trouvé pour le QR '{request.QrCode}'.");

        var previous = item.Status.ToShared();
        var target = request.TargetStatus.HasValue
            ? request.TargetStatus.Value.ToDomain()
            : ItemStateMachine.GetDefaultNextStatus(item.Status);
        if (!target.HasValue)
        {
            return Result.Success(new QrScanResultDto(
                false,
                MapItemToDto(item, includeQrSvg: true),
                item.Order.TicketNumber,
                item.Order.Customer?.FullName ?? string.Empty,
                previous,
                item.Status.ToShared(),
                ItemStateMachine.GetAllowedNextStatuses(item.Status).Select(s => s.ToShared()).ToList()));
        }

        var transition = await TransitionItemAsync(item, target.Value, request.SubStep.ToDomain(), request.StorageLocationId, request.Notes ?? "Scan QR", ct);
        if (transition.IsFailure)
            return Result.Failure<QrScanResultDto>(transition.Error.Code, transition.Error.Message);

        return Result.Success(new QrScanResultDto(
            true,
            transition.Value,
            item.Order.TicketNumber,
            item.Order.Customer?.FullName ?? string.Empty,
            previous,
            item.Status.ToShared(),
            ItemStateMachine.GetAllowedNextStatuses(item.Status).Select(s => s.ToShared()).ToList()));
    }

    private async Task<Result<OrderItemDto>> TransitionItemAsync(
        OrderItem item,
        DomainItemStatus targetStatus,
        DomainTreatmentSubStep subStep,
        Guid? storageLocationId,
        string? notes,
        CancellationToken ct)
    {
        if (!ItemStateMachine.CanTransition(item.Status, targetStatus))
        {
            return Result.Failure<OrderItemDto>(
                "Order.InvalidTransition",
                $"Transition interdite de '{item.Status}' vers '{targetStatus}'.");
        }

        var order = item.Order!;
        var previousItemStatus = item.Status;
        var previousOrderStatus = order.Status;
        var now = DateTimeOffset.UtcNow;

        if (targetStatus == DomainItemStatus.Ready && storageLocationId.HasValue)
        {
            var locationResult = await AssignStorageInternalAsync(item, storageLocationId.Value, ct);
            if (locationResult.IsFailure)
                return Result.Failure<OrderItemDto>(locationResult.Error.Code, locationResult.Error.Message);
        }

        if (targetStatus is DomainItemStatus.Delivered or DomainItemStatus.Cancelled && item.StorageLocationId.HasValue)
        {
            var oldLoc = await orderStore.GetStorageLocationByIdAsync(item.StorageLocationId.Value, ct);
            if (oldLoc is not null)
            {
                oldLoc.IsOccupied = false;
                oldLoc.CurrentOrderItemId = null;
            }
            item.StorageLocationId = null;
        }

        item.Status = targetStatus;
        item.TreatmentSubStep = targetStatus == DomainItemStatus.InTreatment ? subStep : DomainTreatmentSubStep.None;
        if (targetStatus == DomainItemStatus.Ready && item.ReadySince is null)
            item.ReadySince = now;

        item.StatusHistory.Add(new ItemStatusHistory
        {
            BranchId = item.BranchId,
            OrderItemId = item.Id,
            FromStatus = previousItemStatus,
            ToStatus = targetStatus,
            SubStep = item.TreatmentSubStep,
            ChangedBy = currentUser.DisplayName,
            ChangedByUserId = currentUser.UserId,
            ChangedAt = now,
            Notes = notes?.Trim()
        });

        order.RefreshStatus(now);
        await unitOfWork.SaveChangesAsync(ct);

        await eventDispatcher.DispatchAsync(
            new OrderStatusChangedEvent(order.Id, order.TicketNumber, order.CustomerId, previousOrderStatus, order.Status, item.Id, item.Status),
            ct);

        if (order.Customer is not null && targetStatus is DomainItemStatus.InTreatment or DomainItemStatus.QualityCheck or DomainItemStatus.Ready)
        {
            var template = targetStatus switch
            {
                DomainItemStatus.InTreatment => "order.inprogress",
                DomainItemStatus.QualityCheck => "order.quality",
                DomainItemStatus.Ready => "order.ready",
                _ => null
            };
            if (template is not null)
            {
                await notificationService.EnqueueAsync(
                    new NotificationRequest(
                        template,
                        order.CustomerId,
                        null,
                        order.Customer.Email,
                        order.Customer.WhatsAppNumber ?? order.Customer.Phone,
                        new Dictionary<string, string>
                        {
                            ["customer_name"] = order.Customer.FullName,
                            ["ticket"] = order.TicketNumber,
                            ["item"] = $"{item.GarmentName} ({item.ServiceName})",
                            ["balance"] = order.BalanceFcfa.ToString("N0")
                        }),
                    ct);
            }
        }

        await realtimeNotifier.NotifyProductionItemChangedAsync(order.Id, item.Id, item.Status.ToString(), ct);
        await realtimeNotifier.NotifyOrderUpdatedAsync(order.Id, order.TicketNumber, ct);

        return Result.Success(MapItemToDto(item, includeQrSvg: true));
    }

    public async Task<Result<OrderDto>> PickupAsync(Guid orderId, PickupOrderRequest request, CancellationToken ct = default)
    {
        var validation = await pickupValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Failure<OrderDto>("Order.ValidationFailed", validation.Errors[0].ErrorMessage);

        var order = await orderStore.GetByIdAsync(orderId, includeDetails: true, ct);
        if (order is null)
            return Result.Failure<OrderDto>("Order.NotFound", "Commande introuvable.");

        if (request.RowVersion.HasValue && order.RowVersion != request.RowVersion.Value)
            return Result.Failure<OrderDto>("Order.ConcurrencyConflict", "Cette commande a été modifiée simultanément par un autre caissier.");

        if (order.Status == DomainOrderStatus.Cancelled)
            return Result.Failure<OrderDto>("Order.AlreadyCancelled", "Impossible de retirer une commande annulée.");

        var eligibleItems = order.Items.Where(i => i.Status is DomainItemStatus.Ready or DomainItemStatus.Unclaimed).ToList();
        if (eligibleItems.Count == 0)
            return Result.Failure<OrderDto>("Order.NoItemsReadyForPickup", "Aucun article prêt pour le retrait.");

        order.PaidFcfa += request.PaymentCollectedFcfa;

        // RG-03: pickup blocked if balance remains unless authorized manager/admin override
        if (order.BalanceFcfa > 0m)
        {
            if (!request.OverrideUnpaidBalance)
            {
                return Result.Failure<OrderDto>(
                    "Order.UnpaidBalance",
                    $"Solde restant de {order.BalanceFcfa:N0} FCFA à régler avant le retrait (RG-03).");
            }

            if (!currentUser.IsInRole(Roles.Admin) && !currentUser.IsInRole(Roles.Manager))
            {
                return Result.Failure<OrderDto>(
                    "Order.OverrideForbidden",
                    "Seul un Administrateur ou un Manager peut autoriser un retrait avec solde impayé.");
            }

            order.BalanceOverrideUsed = true;
            order.BalanceOverrideReason = request.OverrideReason?.Trim();
            order.BalanceOverrideBy = currentUser.DisplayName;
        }

        var now = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.SignatureDataUrl))
        {
            var sig = await fileStorage.SaveBase64ImageAsync(request.SignatureDataUrl, "signatures", order.TicketNumber, ct);
            order.PickupSignaturePath = sig.PublicUrl;
        }

        foreach (var item in eligibleItems)
        {
            var prev = item.Status;
            item.Status = DomainItemStatus.Delivered;
            if (item.StorageLocationId.HasValue)
            {
                var loc = await orderStore.GetStorageLocationByIdAsync(item.StorageLocationId.Value, ct);
                if (loc is not null)
                {
                    loc.IsOccupied = false;
                    loc.CurrentOrderItemId = null;
                }
                item.StorageLocationId = null;
            }
            item.StatusHistory.Add(new ItemStatusHistory
            {
                BranchId = item.BranchId,
                OrderItemId = item.Id,
                FromStatus = prev,
                ToStatus = DomainItemStatus.Delivered,
                ChangedBy = currentUser.DisplayName,
                ChangedByUserId = currentUser.UserId,
                ChangedAt = now,
                Notes = order.BalanceOverrideUsed
                    ? $"Retrait avec dérogation solde ({order.BalanceOverrideReason})"
                    : "Retrait client signé"
            });
        }

        order.RefreshStatus(now);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogAsync(
            "OrderPickedUp",
            nameof(Order),
            order.Id,
            null,
            new { order.TicketNumber, request.PaymentCollectedFcfa, order.BalanceFcfa, order.BalanceOverrideUsed, order.BalanceOverrideReason },
            ct);

        await eventDispatcher.DispatchAsync(
            new OrderPickedUpEvent(order.Id, order.TicketNumber, order.CustomerId, request.PaymentCollectedFcfa, order.BalanceOverrideUsed),
            ct);

        await realtimeNotifier.NotifyOrderUpdatedAsync(order.Id, order.TicketNumber, ct);
        return Result.Success(MapToDto(order));
    }

    public async Task<Result<OrderDto>> CancelAsync(Guid orderId, CancelOrderRequest request, CancellationToken ct = default)
    {
        var validation = await cancelValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Failure<OrderDto>("Order.ValidationFailed", validation.Errors[0].ErrorMessage);

        var order = await orderStore.GetByIdAsync(orderId, includeDetails: true, ct);
        if (order is null)
            return Result.Failure<OrderDto>("Order.NotFound", "Commande introuvable.");

        if (request.RowVersion.HasValue && order.RowVersion != request.RowVersion.Value)
            return Result.Failure<OrderDto>("Order.ConcurrencyConflict", "Cette commande a été modifiée simultanément.");

        if (order.Status == DomainOrderStatus.Cancelled)
            return Result.Failure<OrderDto>("Order.AlreadyCancelled", "Cette commande est déjà annulée.");

        if (order.Items.Any(i => !ItemStateMachine.CanTransition(i.Status, DomainItemStatus.Cancelled) && i.Status != DomainItemStatus.Cancelled))
        {
            return Result.Failure<OrderDto>(
                "Order.CannotCancelAfterTreatmentStarted",
                "Annulation impossible : au moins un article est déjà en traitement, prêt, livré ou aux enchères.");
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var item in order.Items.Where(i => i.Status != DomainItemStatus.Cancelled))
        {
            var prev = item.Status;
            item.Status = DomainItemStatus.Cancelled;
            item.StatusHistory.Add(new ItemStatusHistory
            {
                BranchId = item.BranchId,
                OrderItemId = item.Id,
                FromStatus = prev,
                ToStatus = DomainItemStatus.Cancelled,
                ChangedBy = currentUser.DisplayName,
                ChangedByUserId = currentUser.UserId,
                ChangedAt = now,
                Notes = $"Annulation : {request.Reason.Trim()}"
            });
        }

        order.CancellationReason = request.Reason.Trim();
        order.CancelledBy = currentUser.DisplayName;
        order.CancelledAt = now;
        order.RefreshStatus(now);

        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogAsync(
            "OrderCancelled",
            nameof(Order),
            order.Id,
            null,
            new { order.TicketNumber, order.CancellationReason, order.CancelledBy },
            ct);

        await eventDispatcher.DispatchAsync(
            new OrderCancelledEvent(order.Id, order.TicketNumber, order.CustomerId, order.CancellationReason),
            ct);

        await realtimeNotifier.NotifyOrderUpdatedAsync(order.Id, order.TicketNumber, ct);
        return Result.Success(MapToDto(order));
    }

    public async Task<Result<ClaimDto>> OpenClaimAsync(Guid orderId, CreateClaimRequest request, CancellationToken ct = default)
    {
        var validation = await claimValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Failure<ClaimDto>("Claim.ValidationFailed", validation.Errors[0].ErrorMessage);

        var order = await orderStore.GetByIdAsync(orderId, includeDetails: true, ct);
        if (order is null)
            return Result.Failure<ClaimDto>("Order.NotFound", "Commande introuvable.");

        string? photoPath = null;
        string? photoUrl = null;
        if (!string.IsNullOrWhiteSpace(request.PhotoDataUrl))
        {
            var stored = await fileStorage.SaveBase64ImageAsync(request.PhotoDataUrl, "claims", order.TicketNumber, ct);
            photoPath = stored.StoragePath;
            photoUrl = stored.PublicUrl;
        }

        var claim = new Claim
        {
            BranchId = order.BranchId,
            OrderId = order.Id,
            OrderItemId = request.OrderItemId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Status = DomainClaimStatus.Open,
            PhotoStoragePath = photoPath,
            PhotoPublicUrl = photoUrl,
            OpenedBy = currentUser.DisplayName,
            OpenedAt = DateTimeOffset.UtcNow
        };

        await orderStore.AddClaimAsync(claim, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogAsync("ClaimOpened", nameof(Claim), claim.Id, null, new { order.TicketNumber, claim.Title }, ct);
        await eventDispatcher.DispatchAsync(new ClaimOpenedEvent(claim.Id, order.Id, order.TicketNumber, claim.Title), ct);

        return Result.Success(MapClaimToDto(claim));
    }

    public async Task<Result<ClaimDto>> ResolveClaimAsync(Guid claimId, ResolveClaimRequest request, CancellationToken ct = default)
    {
        var validation = await resolveClaimValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Failure<ClaimDto>("Claim.ValidationFailed", validation.Errors[0].ErrorMessage);

        var claim = await orderStore.GetClaimByIdAsync(claimId, ct);
        if (claim is null)
            return Result.Failure<ClaimDto>("Claim.NotFound", "Réclamation introuvable.");

        claim.Status = request.Status.ToDomain();
        claim.ResolutionNotes = request.ResolutionNotes.Trim();
        claim.GoodwillGestureFcfa = request.GoodwillGestureFcfa;
        claim.ResolvedBy = currentUser.DisplayName;
        claim.ResolvedAt = DateTimeOffset.UtcNow;

        await unitOfWork.SaveChangesAsync(ct);
        await auditService.LogAsync("ClaimResolved", nameof(Claim), claim.Id, null, request, ct);
        return Result.Success(MapClaimToDto(claim));
    }

    public async Task<IReadOnlyList<StorageLocationDto>> GetStorageLocationsAsync(
        bool? occupiedOnly = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var list = await orderStore.GetStorageLocationsAsync(occupiedOnly, search, ct);
        var result = new List<StorageLocationDto>(list.Count);
        foreach (var loc in list)
        {
            string? ticket = null;
            string? itemLabel = null;
            if (loc.CurrentOrderItemId.HasValue)
            {
                var item = await orderStore.GetItemByIdAsync(loc.CurrentOrderItemId.Value, ct);
                if (item is not null)
                {
                    ticket = item.Order?.TicketNumber;
                    itemLabel = $"{item.GarmentName} · {item.ServiceName}";
                }
            }
            result.Add(new StorageLocationDto(
                loc.Id, loc.Aisle, loc.Rack, loc.HangerNumber, loc.Code,
                loc.IsOccupied, loc.CurrentOrderItemId, ticket, itemLabel));
        }
        return result;
    }

    public async Task<Result<StorageLocationDto>> CreateStorageLocationAsync(CreateStorageLocationRequest request, CancellationToken ct = default)
    {
        var validation = await storageValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Failure<StorageLocationDto>("Storage.ValidationFailed", validation.Errors[0].ErrorMessage);

        var code = StorageLocation.BuildCode(request.Aisle, request.Rack, request.HangerNumber);
        if (await orderStore.StorageLocationCodeExistsAsync(code, ct))
            return Result.Failure<StorageLocationDto>("Storage.DuplicateCode", $"L'emplacement '{code}' existe déjà.");

        var loc = new StorageLocation
        {
            BranchId = currentUser.BranchId,
            Aisle = request.Aisle.Trim().ToUpperInvariant(),
            Rack = request.Rack.Trim().ToUpperInvariant(),
            HangerNumber = request.HangerNumber.Trim().ToUpperInvariant(),
            Code = code
        };

        await orderStore.AddStorageLocationAsync(loc, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new StorageLocationDto(
            loc.Id, loc.Aisle, loc.Rack, loc.HangerNumber, loc.Code, false, null, null, null));
    }

    public async Task<Result<OrderItemDto>> AssignStorageAsync(Guid itemId, AssignStorageRequest request, CancellationToken ct = default)
    {
        var item = await orderStore.GetItemByIdAsync(itemId, ct);
        if (item is null)
            return Result.Failure<OrderItemDto>("OrderItem.NotFound", "Article introuvable.");

        var result = await AssignStorageInternalAsync(item, request.StorageLocationId, ct);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(ct);
        await realtimeNotifier.NotifyProductionItemChangedAsync(item.OrderId, item.Id, item.Status.ToString(), ct);
        return Result.Success(MapItemToDto(item, includeQrSvg: true));
    }

    private async Task<Result<OrderItemDto>> AssignStorageInternalAsync(OrderItem item, Guid storageLocationId, CancellationToken ct)
    {
        var target = await orderStore.GetStorageLocationByIdAsync(storageLocationId, ct);
        if (target is null)
            return Result.Failure<OrderItemDto>("Storage.NotFound", "Emplacement introuvable.");

        if (target.IsOccupied && target.CurrentOrderItemId != item.Id)
            return Result.Failure<OrderItemDto>("Storage.AlreadyOccupied", $"L'emplacement '{target.Code}' est déjà occupé.");

        if (item.StorageLocationId.HasValue && item.StorageLocationId.Value != target.Id)
        {
            var old = await orderStore.GetStorageLocationByIdAsync(item.StorageLocationId.Value, ct);
            if (old is not null)
            {
                old.IsOccupied = false;
                old.CurrentOrderItemId = null;
            }
        }

        target.IsOccupied = true;
        target.CurrentOrderItemId = item.Id;
        item.StorageLocationId = target.Id;
        item.StorageLocation = target;
        return Result.Success(MapItemToDto(item, includeQrSvg: false));
    }

    public OrderDto MapToDto(Order o) => new(
        o.Id,
        o.TicketNumber,
        o.TrackingCode,
        o.CustomerId,
        o.Customer?.FullName ?? string.Empty,
        o.Customer?.Phone ?? string.Empty,
        o.Status.ToShared(),
        o.DeliveryOption.ToShared(),
        o.DeliveryZoneId,
        o.DeliveryAddress,
        o.IsExpress,
        o.AcceptedTermsAndConditions,
        o.TermsAcceptedAt,
        o.TermsSummary,
        o.SubtotalFcfa,
        o.DeliveryFeeFcfa,
        o.StorageFeeFcfa,
        o.DiscountFcfa,
        o.TotalFcfa,
        o.PaidFcfa,
        o.BalanceFcfa,
        o.PromisedAt,
        o.EstimatedReadyAt,
        o.ReadyAt,
        o.PickedUpAt,
        o.PickupSignaturePath,
        o.BalanceOverrideUsed,
        o.BalanceOverrideReason,
        o.Notes,
        o.CancellationReason,
        o.Items.OrderBy(i => i.QrCode).Select(i => MapItemToDto(i, includeQrSvg: true)).ToList(),
        o.Claims.OrderByDescending(c => c.OpenedAt).Select(MapClaimToDto).ToList(),
        o.CreatedAt,
        o.RowVersion);

    private OrderItemDto MapItemToDto(OrderItem i, bool includeQrSvg) => new(
        i.Id,
        i.OrderId,
        i.QrCode,
        includeQrSvg ? qrCodeService.GenerateSvg(i.QrCode) : null,
        i.GarmentCode,
        i.GarmentName,
        i.ServiceCode,
        i.ServiceName,
        i.Status.ToShared(),
        i.TreatmentSubStep.ToShared(),
        i.IsExpress,
        i.IsBulky,
        i.Color,
        i.Brand,
        i.GetDefectTags(),
        i.DefectNotes,
        i.BasePriceFcfa,
        i.SurchargeFcfa,
        i.DiscountFcfa,
        i.FinalPriceFcfa,
        i.StorageLocationId,
        i.StorageLocation?.Code,
        i.Photos.Select(p => new ItemPhotoDto(p.Id, p.FileName, p.PublicUrl, p.Caption, p.CapturedAt)).ToList(),
        i.StatusHistory.OrderBy(h => h.ChangedAt)
            .Select(h => new ItemStatusHistoryDto(h.Id, h.FromStatus?.ToShared(), h.ToStatus.ToShared(), h.SubStep.ToShared(), h.ChangedBy, h.ChangedAt, h.Notes))
            .ToList(),
        i.RowVersion);

    private static ClaimDto MapClaimToDto(Claim c) => new(
        c.Id,
        c.OrderId,
        c.OrderItemId,
        c.Title,
        c.Description,
        c.Status.ToShared(),
        c.PhotoPublicUrl,
        c.ResolutionNotes,
        c.GoodwillGestureFcfa,
        c.OpenedBy,
        c.OpenedAt,
        c.ResolvedBy,
        c.ResolvedAt);

    private static string GenerateSecretCode(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(length);
        var result = new char[length];
        for (var i = 0; i < length; i++)
        {
            result[i] = chars[bytes[i] % chars.Length];
        }
        return new string(result);
    }

    private static string GenerateNumericOtp(int digits)
    {
        var max = (int)Math.Pow(10, digits);
        return RandomNumberGenerator.GetInt32(0, max).ToString($"D{digits}");
    }
}

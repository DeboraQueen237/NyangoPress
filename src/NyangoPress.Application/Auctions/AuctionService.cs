using FluentValidation;
using NyangoPress.Application.Common;
using NyangoPress.Domain.Auctions;
using NyangoPress.Domain.Orders;
using NyangoPress.Shared.Auctions;
using NyangoPress.Shared.Orders;
using DomainAuctionLotStatus = NyangoPress.Domain.Auctions.AuctionLotStatus;
using DomainItemStatus = NyangoPress.Domain.Orders.ItemStatus;
using SharedAuctionLotStatus = NyangoPress.Shared.Auctions.AuctionLotStatus;

namespace NyangoPress.Application.Auctions;

public interface IAuctionService
{
    Task<IReadOnlyList<UnclaimedCandidateDto>> GetUnclaimedCandidatesAsync(int thresholdDays = 60, int freeStorageDays = 7, decimal dailyFeeFcfa = 200m, CancellationToken ct = default);
    Task<Result<DetectUnclaimedResultDto>> DetectAndMarkUnclaimedAsync(DetectUnclaimedRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<AuctionLotDto>> GetLotsAsync(SharedAuctionLotStatus? status = null, CancellationToken ct = default);
    Task<Result<AuctionLotDto>> GetLotByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<AuctionLotDto>> CreateLotAsync(CreateAuctionLotRequest request, CancellationToken ct = default);
    Task<Result<AuctionLotDto>> OpenLotAsync(Guid lotId, CancellationToken ct = default);
    Task<Result<AuctionLotDto>> PlaceBidAsync(Guid lotId, PlaceBidRequest request, CancellationToken ct = default);
    Task<Result<AuctionLotDto>> CloseAndAwardLotAsync(Guid lotId, AwardAuctionLotRequest request, CancellationToken ct = default);
}

public sealed class CreateAuctionLotValidator : AbstractValidator<CreateAuctionLotRequest>
{
    public CreateAuctionLotValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.StartingPriceFcfa).GreaterThan(0m);
        RuleFor(x => x.MinimumIncrementFcfa).GreaterThan(0m);
        RuleFor(x => x.OrderItemIds).NotEmpty();
    }
}

public sealed class PlaceBidValidator : AbstractValidator<PlaceBidRequest>
{
    public PlaceBidValidator()
    {
        RuleFor(x => x.BidderName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.BidderPhone).NotEmpty().MinimumLength(6).MaximumLength(30);
        RuleFor(x => x.AmountFcfa).GreaterThan(0m);
    }
}

public sealed class AuctionService(
    IAuctionStore store,
    ISequenceGenerator sequenceGenerator,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    IDomainEventDispatcher eventDispatcher,
    INotificationService notificationService,
    ICurrentUser currentUser,
    IValidator<CreateAuctionLotRequest> createValidator,
    IValidator<PlaceBidRequest> bidValidator) : IAuctionService
{
    public async Task<IReadOnlyList<UnclaimedCandidateDto>> GetUnclaimedCandidatesAsync(
        int thresholdDays = 60,
        int freeStorageDays = 7,
        decimal dailyFeeFcfa = 200m,
        CancellationToken ct = default)
    {
        var items = await store.GetUnclaimedEligibleItemsAsync(thresholdDays, ct);
        var now = DateTimeOffset.UtcNow;

        return items.Select(i =>
        {
            var readySince = i.ReadySince ?? i.Order!.ReadyAt ?? i.Order.CreatedAt;
            var days = Math.Max(0, (int)(now - readySince).TotalDays);
            var chargeableDays = Math.Max(0, days - freeStorageDays);
            var storageFee = chargeableDays * dailyFeeFcfa;
            return new UnclaimedCandidateDto(
                i.Id,
                i.OrderId,
                i.Order!.TicketNumber,
                i.QrCode,
                i.Order.Customer?.FullName ?? string.Empty,
                i.Order.Customer?.Phone ?? string.Empty,
                i.GarmentName,
                i.ServiceName,
                i.Status.ToShared(),
                readySince,
                days,
                i.Order.BalanceFcfa,
                storageFee,
                i.Order.AcceptedTermsAndConditions);
        }).ToList();
    }

    public async Task<Result<DetectUnclaimedResultDto>> DetectAndMarkUnclaimedAsync(DetectUnclaimedRequest request, CancellationToken ct = default)
    {
        var items = await store.GetUnclaimedEligibleItemsAsync(request.UnclaimedThresholdDays, ct);
        var now = DateTimeOffset.UtcNow;
        var marked = 0;

        if (request.ApplyStatusTransition)
        {
            var touchedOrders = new HashSet<Order>();
            foreach (var item in items.Where(i => i.Status == DomainItemStatus.Ready && i.Order?.AcceptedTermsAndConditions == true))
            {
                if (!ItemStateMachine.CanTransition(item.Status, DomainItemStatus.Unclaimed))
                    continue;

                var readySince = item.ReadySince ?? item.Order!.ReadyAt ?? item.Order.CreatedAt;
                var days = Math.Max(0, (int)(now - readySince).TotalDays);
                var chargeableDays = Math.Max(0, days - request.FreeStorageDays);
                var storageFee = chargeableDays * request.DailyStorageFeeFcfa;

                item.Status = DomainItemStatus.Unclaimed;
                item.StatusHistory.Add(new ItemStatusHistory
                {
                    BranchId = item.BranchId,
                    OrderItemId = item.Id,
                    FromStatus = DomainItemStatus.Ready,
                    ToStatus = DomainItemStatus.Unclaimed,
                    ChangedBy = currentUser.DisplayName,
                    ChangedByUserId = currentUser.UserId,
                    ChangedAt = now,
                    Notes = $"Marqué non réclamé après {days} jours (frais de garde estimés : {storageFee:N0} FCFA)"
                });

                if (item.Order is not null)
                {
                    item.Order.StorageFeeFcfa = Math.Max(item.Order.StorageFeeFcfa, storageFee);
                    touchedOrders.Add(item.Order);
                }

                marked++;
            }

            foreach (var order in touchedOrders)
            {
                order.RefreshStatus(now);
                if (order.Customer is not null)
                {
                    await notificationService.EnqueueAsync(
                        new NotificationRequest(
                            "order.overdue.final",
                            order.CustomerId,
                            null,
                            order.Customer.Email,
                            order.Customer.WhatsAppNumber ?? order.Customer.Phone,
                            new Dictionary<string, string>
                            {
                                ["customer_name"] = order.Customer.FullName,
                                ["ticket"] = order.TicketNumber,
                                ["storage_fee"] = order.StorageFeeFcfa.ToString("N0")
                            }),
                        ct);
                }
            }

            if (marked > 0)
            {
                await unitOfWork.SaveChangesAsync(ct);
                await auditService.LogAsync("UnclaimedItemsDetected", nameof(OrderItem), Guid.Empty, null, new { marked }, ct);
            }
        }

        var candidates = await GetUnclaimedCandidatesAsync(request.UnclaimedThresholdDays, request.FreeStorageDays, request.DailyStorageFeeFcfa, ct);
        return Result.Success(new DetectUnclaimedResultDto(marked, candidates));
    }

    public async Task<IReadOnlyList<AuctionLotDto>> GetLotsAsync(SharedAuctionLotStatus? status = null, CancellationToken ct = default)
        => (await store.GetLotsAsync(status, ct)).Select(MapLot).ToList();

    public async Task<Result<AuctionLotDto>> GetLotByIdAsync(Guid id, CancellationToken ct = default)
    {
        var lot = await store.GetLotByIdAsync(id, ct);
        return lot is null
            ? Result.Failure<AuctionLotDto>("Auction.LotNotFound", "Lot d'enchères introuvable.")
            : Result.Success(MapLot(lot));
    }

    public async Task<Result<AuctionLotDto>> CreateLotAsync(CreateAuctionLotRequest request, CancellationToken ct = default)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Failure<AuctionLotDto>("Auction.ValidationFailed", validation.Errors[0].ErrorMessage);

        var items = await store.GetItemsByIdsAsync(request.OrderItemIds, ct);
        if (items.Count == 0)
            return Result.Failure<AuctionLotDto>("Auction.NoValidItems", "Aucun article non réclamé valide sélectionné.");

        if (items.Any(i => i.Status != DomainItemStatus.Unclaimed || i.AuctionLotId.HasValue || !(i.Order?.AcceptedTermsAndConditions ?? false)))
        {
            return Result.Failure<AuctionLotDto>(
                "Auction.ItemsNotEligible",
                "Seuls les articles au statut 'Non réclamé' avec CGV acceptées et non affectés à un autre lot peuvent être mis aux enchères.");
        }

        var now = DateTimeOffset.UtcNow;
        var lot = new AuctionLot
        {
            BranchId = currentUser.BranchId,
            LotNumber = await sequenceGenerator.NextAuctionLotNumberAsync(ct),
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Status = request.OpenImmediately ? DomainAuctionLotStatus.Open : DomainAuctionLotStatus.Draft,
            StartingPriceFcfa = request.StartingPriceFcfa,
            CurrentHighestBidFcfa = 0m,
            MinimumIncrementFcfa = request.MinimumIncrementFcfa,
            OpensAt = request.OpenImmediately ? now : null,
            ClosesAt = request.ClosesAt
        };

        foreach (var item in items)
        {
            item.AuctionLotId = lot.Id;
            lot.Items.Add(item);
        }

        await store.AddLotAsync(lot, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await auditService.LogAsync("AuctionLotCreated", nameof(AuctionLot), lot.Id, null, new { lot.LotNumber, lot.Title, Items = items.Count }, ct);
        return Result.Success(MapLot(lot));
    }

    public async Task<Result<AuctionLotDto>> OpenLotAsync(Guid lotId, CancellationToken ct = default)
    {
        var lot = await store.GetLotByIdAsync(lotId, ct);
        if (lot is null)
            return Result.Failure<AuctionLotDto>("Auction.LotNotFound", "Lot introuvable.");
        if (lot.Status != DomainAuctionLotStatus.Draft)
            return Result.Failure<AuctionLotDto>("Auction.InvalidState", "Seul un lot en brouillon peut être ouvert.");

        lot.Status = DomainAuctionLotStatus.Open;
        lot.OpensAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(MapLot(lot));
    }

    public async Task<Result<AuctionLotDto>> PlaceBidAsync(Guid lotId, PlaceBidRequest request, CancellationToken ct = default)
    {
        var validation = await bidValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Failure<AuctionLotDto>("Auction.ValidationFailed", validation.Errors[0].ErrorMessage);

        var lot = await store.GetLotByIdAsync(lotId, ct);
        if (lot is null)
            return Result.Failure<AuctionLotDto>("Auction.LotNotFound", "Lot introuvable.");

        if (lot.Status != DomainAuctionLotStatus.Open)
            return Result.Failure<AuctionLotDto>("Auction.NotOpen", "Ce lot n'est pas ouvert aux enchères.");

        var minAllowed = lot.Bids.Count == 0
            ? lot.StartingPriceFcfa
            : lot.CurrentHighestBidFcfa + lot.MinimumIncrementFcfa;

        if (request.AmountFcfa < minAllowed)
        {
            return Result.Failure<AuctionLotDto>(
                "Auction.BidTooLow",
                $"L'offre minimum est de {minAllowed:N0} FCFA.");
        }

        foreach (var existing in lot.Bids)
            existing.IsWinning = false;

        var bid = new AuctionBid
        {
            BranchId = lot.BranchId,
            AuctionLotId = lot.Id,
            BidderName = request.BidderName.Trim(),
            BidderPhone = request.BidderPhone.Trim(),
            AmountFcfa = request.AmountFcfa,
            PlacedAt = DateTimeOffset.UtcNow,
            IsWinning = true
        };

        lot.CurrentHighestBidFcfa = request.AmountFcfa;
        lot.WinningBidderName = bid.BidderName;
        lot.WinningBidderPhone = bid.BidderPhone;
        lot.Bids.Add(bid);

        await store.AddBidAsync(bid, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(MapLot(lot));
    }

    public async Task<Result<AuctionLotDto>> CloseAndAwardLotAsync(Guid lotId, AwardAuctionLotRequest request, CancellationToken ct = default)
    {
        var lot = await store.GetLotByIdAsync(lotId, ct);
        if (lot is null)
            return Result.Failure<AuctionLotDto>("Auction.LotNotFound", "Lot introuvable.");

        if (lot.Status is DomainAuctionLotStatus.Awarded or DomainAuctionLotStatus.Cancelled)
            return Result.Failure<AuctionLotDto>("Auction.AlreadyFinalized", "Ce lot est déjà clôturé/adjugé.");

        var now = DateTimeOffset.UtcNow;
        var winningBid = lot.Bids.OrderByDescending(b => b.AmountFcfa).FirstOrDefault();

        if (winningBid is null)
        {
            lot.Status = DomainAuctionLotStatus.Closed;
            lot.AwardNotes = request.Notes?.Trim() ?? "Clôturé sans offre";
        }
        else
        {
            lot.Status = DomainAuctionLotStatus.Awarded;
            lot.WinningBidderName = winningBid.BidderName;
            lot.WinningBidderPhone = winningBid.BidderPhone;
            lot.FinalSalePriceFcfa = winningBid.AmountFcfa;
            lot.AwardedAt = now;
            lot.AwardNotes = request.Notes?.Trim();

            var touchedOrders = new HashSet<Order>();
            foreach (var item in lot.Items)
            {
                if (ItemStateMachine.CanTransition(item.Status, DomainItemStatus.Auctioned))
                {
                    item.Status = DomainItemStatus.Auctioned;
                    item.StatusHistory.Add(new ItemStatusHistory
                    {
                        BranchId = item.BranchId,
                        OrderItemId = item.Id,
                        FromStatus = DomainItemStatus.Unclaimed,
                        ToStatus = DomainItemStatus.Auctioned,
                        ChangedBy = currentUser.DisplayName,
                        ChangedByUserId = currentUser.UserId,
                        ChangedAt = now,
                        Notes = $"Adjugé lot {lot.LotNumber} à {winningBid.BidderName} ({winningBid.AmountFcfa:N0} FCFA)"
                    });
                    if (item.Order is not null) touchedOrders.Add(item.Order);
                }
            }

            foreach (var order in touchedOrders)
                order.RefreshStatus(now);
        }

        await unitOfWork.SaveChangesAsync(ct);
        await auditService.LogAsync("AuctionLotClosed", nameof(AuctionLot), lot.Id, null, new { lot.LotNumber, lot.Status, lot.FinalSalePriceFcfa }, ct);
        await eventDispatcher.DispatchAsync(
            new AuctionLotAwardedEvent(lot.Id, lot.LotNumber, lot.WinningBidderName, lot.FinalSalePriceFcfa ?? 0m),
            ct);

        return Result.Success(MapLot(lot));
    }

    private static AuctionLotDto MapLot(AuctionLot l) => new(
        l.Id,
        l.LotNumber,
        l.Title,
        l.Description,
        l.Status.ToShared(),
        l.StartingPriceFcfa,
        l.CurrentHighestBidFcfa,
        l.MinimumIncrementFcfa,
        l.WinningBidderName,
        l.WinningBidderPhone,
        l.FinalSalePriceFcfa,
        l.OpensAt,
        l.ClosesAt,
        l.AwardedAt,
        l.Items.Select(i => new AuctionLotItemDto(
            i.Id,
            i.Order?.TicketNumber ?? string.Empty,
            i.QrCode,
            i.GarmentName,
            i.ServiceName,
            i.Color,
            i.Brand)).ToList(),
        l.Bids.OrderByDescending(b => b.AmountFcfa)
            .Select(b => new AuctionBidDto(b.Id, b.AuctionLotId, b.BidderName, b.BidderPhone, b.AmountFcfa, b.PlacedAt, b.IsWinning))
            .ToList(),
        l.RowVersion);
}

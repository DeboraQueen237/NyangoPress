namespace NyangoPress.Domain.Orders;

public enum ItemStatus
{
    Received = 0,
    Sorted = 1,
    InTreatment = 2,
    QualityCheck = 3,
    Ready = 4,
    Delivered = 5,
    Unclaimed = 6,
    Auctioned = 7,
    Cancelled = 8
}

public enum TreatmentSubStep
{
    None = 0,
    StainRemoval = 1,
    Washing = 2,
    DryCleaning = 3,
    Drying = 4,
    Pressing = 5,
    Finishing = 6
}

public enum OrderStatus
{
    Received = 0,
    InProgress = 1,
    PartiallyReady = 2,
    Ready = 3,
    Delivered = 4,
    PartiallyUnclaimed = 5,
    Unclaimed = 6,
    Cancelled = 7
}

public enum DeliveryOption
{
    StorePickup = 0,
    HomeDelivery = 1,
    CollectAndDeliver = 2
}

public enum ClaimStatus
{
    Open = 0,
    InReview = 1,
    Resolved = 2,
    Rejected = 3
}

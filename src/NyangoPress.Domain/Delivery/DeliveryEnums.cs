namespace NyangoPress.Domain.Delivery;

public enum DeliveryMissionType
{
    Pickup = 0,
    Delivery = 1
}

public enum DeliveryStatus
{
    Pending = 0,
    Assigned = 1,
    EnRoute = 2,
    Arrived = 3,
    NearDestination = 4,
    Delivered = 5,
    Failed = 6,
    Cancelled = 7
}

public enum PickupRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    ConvertedToOrder = 3
}

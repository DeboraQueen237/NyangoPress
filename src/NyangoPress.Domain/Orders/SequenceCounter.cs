namespace NyangoPress.Domain.Orders;

/// <summary>
/// Transactional gapless sequence counter for ticket numbers, customer codes, and lot numbers.
/// </summary>
public class SequenceCounter
{
    public string Key { get; set; } = string.Empty;
    public long LastValue { get; set; }
}

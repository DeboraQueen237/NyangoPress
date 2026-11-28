using NyangoPress.Domain.Common;

namespace NyangoPress.Application.Common;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct = default);
}

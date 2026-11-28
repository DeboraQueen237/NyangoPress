using NyangoPress.Domain.Customers;
using NyangoPress.Shared.Common;
using NyangoPress.Shared.Customers;

namespace NyangoPress.Application.Customers;

public interface ICustomerStore
{
    Task<Customer?> GetByIdAsync(Guid id, bool includeDetails = true, CancellationToken ct = default);
    Task<PagedResult<CustomerSummaryDto>> SearchAsync(CustomerQueryParams query, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> FindPotentialDuplicatesAsync(string fullName, string phone, Guid? excludeCustomerId = null, CancellationToken ct = default);
    Task<CustomerHistoryDto?> GetHistoryAsync(Guid customerId, CancellationToken ct = default);
    Task AddAsync(Customer customer, CancellationToken ct = default);
    Task ReassignOrdersToCustomerAsync(Guid sourceCustomerId, Guid targetCustomerId, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> GetAllForExportAsync(CancellationToken ct = default);
}

using Microsoft.EntityFrameworkCore;
using NyangoPress.Application.Common;
using NyangoPress.Application.Customers;
using NyangoPress.Domain.Customers;
using NyangoPress.Infrastructure.Persistence;
using NyangoPress.Shared.Common;
using NyangoPress.Shared.Customers;

namespace NyangoPress.Infrastructure.Customers;

public sealed class CustomerStore(AppDbContext db) : ICustomerStore
{
    public Task<Customer?> GetByIdAsync(Guid id, bool includeDetails = true, CancellationToken ct = default)
    {
        IQueryable<Customer> query = db.Customers;
        if (includeDetails)
        {
            query = query
                .Include(c => c.ContactPreference)
                .Include(c => c.CompanyContract);
        }

        return query.FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<PagedResult<CustomerSummaryDto>> SearchAsync(CustomerQueryParams query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = db.Customers
            .AsNoTracking()
            .Include(c => c.ContactPreference)
            .AsQueryable();

        if (query.Type.HasValue)
        {
            var domainType = query.Type.Value.ToDomain();
            q = q.Where(c => c.CustomerType == domainType);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            var norm = Customer.NormalizeText(term);
            var digits = Customer.NormalizePhoneDigits(term);

            q = q.Where(c =>
                c.NormalizedName.Contains(norm) ||
                c.CustomerCode.Contains(term) ||
                c.Phone.Contains(term) ||
                (!string.IsNullOrEmpty(digits) && c.NormalizedPhone.Contains(digits)) ||
                (c.Email != null && c.Email.Contains(term)) ||
                c.Orders.Any(o => o.TicketNumber.Contains(term) || o.Items.Any(i => i.QrCode.Contains(term))));
        }

        var totalCount = await q.CountAsync(ct);

        q = (query.SortBy.ToLowerInvariant(), query.SortDesc) switch
        {
            ("created", true) => q.OrderByDescending(c => c.CreatedAt),
            ("created", false) => q.OrderBy(c => c.CreatedAt),
            ("name", true) => q.OrderByDescending(c => c.FullName),
            _ => q.OrderBy(c => c.FullName)
        };

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerSummaryDto(
                c.Id,
                c.CustomerCode,
                c.CustomerType.ToShared(),
                c.FullName,
                c.Phone,
                c.WhatsAppNumber,
                c.Email,
                c.ContactPreference.PreferredLanguage.ToShared(),
                c.Orders.Count(o => !o.IsDeleted),
                c.Orders.Where(o => !o.IsDeleted).Sum(o => o.TotalFcfa + o.StorageFeeFcfa - o.PaidFcfa),
                c.LoyaltyPoints,
                c.CreatedAt,
                c.RowVersion))
            .ToListAsync(ct);

        return new PagedResult<CustomerSummaryDto>(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<Customer>> FindPotentialDuplicatesAsync(
        string fullName,
        string phone,
        Guid? excludeCustomerId = null,
        CancellationToken ct = default)
    {
        var normPhone = Customer.NormalizePhoneDigits(phone);
        var normName = Customer.NormalizeText(fullName);

        var q = db.Customers.AsNoTracking().AsQueryable();
        if (excludeCustomerId.HasValue)
            q = q.Where(c => c.Id != excludeCustomerId.Value);

        if (string.IsNullOrEmpty(normPhone) && string.IsNullOrEmpty(normName))
            return [];

        return await q
            .Where(c =>
                (!string.IsNullOrEmpty(normPhone) && c.NormalizedPhone == normPhone) ||
                (!string.IsNullOrEmpty(normName) && c.NormalizedName == normName))
            .Take(10)
            .ToListAsync(ct);
    }

    public async Task<CustomerHistoryDto?> GetHistoryAsync(Guid customerId, CancellationToken ct = default)
    {
        var customer = await db.Customers
            .AsNoTracking()
            .Include(c => c.ContactPreference)
            .Include(c => c.CompanyContract)
            .FirstOrDefaultAsync(c => c.Id == customerId, ct);

        if (customer is null) return null;

        var orders = await db.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new CustomerOrderHistoryItemDto(
                o.Id,
                o.TicketNumber,
                o.CreatedAt,
                o.PromisedAt,
                o.Status.ToString(),
                o.TotalFcfa + o.StorageFeeFcfa,
                o.PaidFcfa,
                Math.Max(0m, o.TotalFcfa + o.StorageFeeFcfa - o.PaidFcfa),
                o.Items.Count))
            .ToListAsync(ct);

        var claims = await db.Claims
            .AsNoTracking()
            .Include(cl => cl.Order)
            .Where(cl => cl.Order != null && cl.Order.CustomerId == customerId)
            .OrderByDescending(cl => cl.OpenedAt)
            .Select(cl => new CustomerClaimHistoryItemDto(
                cl.Id,
                cl.OrderId,
                cl.Order!.TicketNumber,
                cl.Title,
                cl.Status.ToString(),
                cl.GoodwillGestureFcfa,
                cl.OpenedAt))
            .ToListAsync(ct);

        var totalSpent = orders.Sum(o => o.PaidFcfa);
        var totalOutstanding = orders.Sum(o => o.BalanceFcfa);

        return new CustomerHistoryDto(
            CustomerService.MapToDto(customer),
            orders,
            claims,
            totalSpent,
            totalOutstanding);
    }

    public Task AddAsync(Customer customer, CancellationToken ct = default)
        => db.Customers.AddAsync(customer, ct).AsTask();

    public async Task ReassignOrdersToCustomerAsync(Guid sourceCustomerId, Guid targetCustomerId, CancellationToken ct = default)
    {
        var orders = await db.Orders.Where(o => o.CustomerId == sourceCustomerId).ToListAsync(ct);
        foreach (var order in orders)
            order.CustomerId = targetCustomerId;

        var deliveries = await db.DeliveryOrders.Where(d => d.CustomerId == sourceCustomerId).ToListAsync(ct);
        foreach (var delivery in deliveries)
            delivery.CustomerId = targetCustomerId;
    }

    public async Task<IReadOnlyList<Customer>> GetAllForExportAsync(CancellationToken ct = default)
        => await db.Customers
            .AsNoTracking()
            .Include(c => c.ContactPreference)
            .OrderBy(c => c.CustomerCode)
            .ToListAsync(ct);
}

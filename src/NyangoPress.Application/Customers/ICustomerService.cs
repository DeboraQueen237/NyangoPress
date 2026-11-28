using NyangoPress.Application.Common;
using NyangoPress.Shared.Common;
using NyangoPress.Shared.Customers;

namespace NyangoPress.Application.Customers;

public interface ICustomerService
{
    Task<PagedResult<CustomerSummaryDto>> SearchAsync(CustomerQueryParams query, CancellationToken ct = default);
    Task<Result<CustomerDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<CustomerHistoryDto>> GetHistoryAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<DuplicateMatchDto>> CheckDuplicatesAsync(string fullName, string phone, Guid? excludeId = null, CancellationToken ct = default);
    Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default);
    Task<Result<CustomerDto>> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default);
    Task<Result<CustomerDto>> UpdatePreferencesAsync(Guid id, UpdateContactPreferenceRequest request, CancellationToken ct = default);
    Task<Result<CustomerDto>> UpsertCompanyContractAsync(Guid id, UpsertCompanyContractRequest request, CancellationToken ct = default);
    Task<Result<CustomerDto>> MergeAsync(MergeCustomersRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface ICustomerImportExportService
{
    Task<byte[]> ExportCsvAsync(CancellationToken ct = default);
    Task<byte[]> ExportExcelAsync(CancellationToken ct = default);
    Task<byte[]> GetCsvTemplateAsync(CancellationToken ct = default);
    Task<byte[]> GetExcelTemplateAsync(CancellationToken ct = default);
    Task<Result<CustomerImportResultDto>> ImportAsync(Stream fileStream, string fileName, CancellationToken ct = default);
}

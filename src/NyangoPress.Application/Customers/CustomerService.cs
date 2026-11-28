using FluentValidation;
using NyangoPress.Application.Common;
using NyangoPress.Domain.Customers;
using NyangoPress.Shared.Common;
using NyangoPress.Shared.Customers;
using DomainCustomerType = NyangoPress.Domain.Customers.CustomerType;
using DomainContactChannel = NyangoPress.Domain.Customers.ContactChannel;
using SharedCustomerType = NyangoPress.Shared.Customers.CustomerType;

namespace NyangoPress.Application.Customers;

public sealed class CustomerService(
    ICustomerStore store,
    ISequenceGenerator sequenceGenerator,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    IDomainEventDispatcher eventDispatcher,
    ICurrentUser currentUser,
    IValidator<CreateCustomerRequest> createValidator,
    IValidator<UpdateCustomerRequest> updateValidator,
    IValidator<UpsertCompanyContractRequest> contractValidator,
    IValidator<MergeCustomersRequest> mergeValidator) : ICustomerService
{
    public Task<PagedResult<CustomerSummaryDto>> SearchAsync(CustomerQueryParams query, CancellationToken ct = default)
        => store.SearchAsync(query, ct);

    public async Task<Result<CustomerDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await store.GetByIdAsync(id, includeDetails: true, ct);
        return customer is null
            ? Result.Failure<CustomerDto>("Customer.NotFound", "Client introuvable.")
            : Result.Success(MapToDto(customer));
    }

    public async Task<Result<CustomerHistoryDto>> GetHistoryAsync(Guid id, CancellationToken ct = default)
    {
        var history = await store.GetHistoryAsync(id, ct);
        return history is null
            ? Result.Failure<CustomerHistoryDto>("Customer.NotFound", "Client introuvable.")
            : Result.Success(history);
    }

    public async Task<IReadOnlyList<DuplicateMatchDto>> CheckDuplicatesAsync(
        string fullName,
        string phone,
        Guid? excludeId = null,
        CancellationToken ct = default)
    {
        var matches = await store.FindPotentialDuplicatesAsync(fullName, phone, excludeId, ct);
        var normalizedPhone = Customer.NormalizePhoneDigits(phone);
        var normalizedName = Customer.NormalizeText(fullName);

        return matches.Select(m =>
        {
            var phoneMatch = !string.IsNullOrEmpty(normalizedPhone) && m.NormalizedPhone == normalizedPhone;
            var nameMatch = !string.IsNullOrEmpty(normalizedName) && m.NormalizedName == normalizedName;
            var reason = phoneMatch && nameMatch
                ? "PhoneAndName"
                : phoneMatch ? "SamePhone" : "SimilarName";
            var score = phoneMatch && nameMatch ? 98 : phoneMatch ? 90 : 70;
            return new DuplicateMatchDto(m.Id, m.CustomerCode, m.FullName, m.Phone, m.Email, reason, score);
        }).ToList();
    }

    public async Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Failure<CustomerDto>("Customer.ValidationFailed", validation.Errors[0].ErrorMessage);

        if (!request.IgnoreDuplicateWarning)
        {
            var duplicates = await CheckDuplicatesAsync(request.FullName, request.Phone, null, ct);
            var strongDuplicate = duplicates.FirstOrDefault(d => d.ConfidenceScore >= 90);
            if (strongDuplicate is not null)
            {
                return Result.Failure<CustomerDto>(
                    "Customer.DuplicateDetected",
                    $"Doublon potentiel détecté : {strongDuplicate.FullName} ({strongDuplicate.Phone}).");
            }
        }

        var code = await sequenceGenerator.NextCustomerCodeAsync(ct);
        var customer = new Customer
        {
            BranchId = currentUser.BranchId,
            CustomerCode = code,
            CustomerType = request.CustomerType.ToDomain(),
            FullName = request.FullName.Trim(),
            NormalizedName = Customer.NormalizeText(request.FullName),
            Phone = request.Phone.Trim(),
            NormalizedPhone = Customer.NormalizePhoneDigits(request.Phone),
            WhatsAppNumber = string.IsNullOrWhiteSpace(request.WhatsAppNumber) ? request.Phone.Trim() : request.WhatsAppNumber.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            ContactPreference = new ContactPreference
            {
                BranchId = currentUser.BranchId,
                PreferredLanguage = request.PreferredLanguage.ToDomain(),
                AllowWhatsApp = request.AllowWhatsApp,
                AllowSms = request.AllowSms,
                AllowEmail = request.AllowEmail,
                MarketingConsent = request.MarketingConsent,
                PrimaryChannel = request.AllowWhatsApp ? DomainContactChannel.WhatsApp
                    : request.AllowSms ? DomainContactChannel.Sms
                    : DomainContactChannel.Email
            }
        };

        await store.AddAsync(customer, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await auditService.LogAsync("CustomerCreated", nameof(Customer), customer.Id, null, new { customer.CustomerCode, customer.FullName, customer.Phone }, ct);
        await eventDispatcher.DispatchAsync(new CustomerCreatedEvent(customer.Id, customer.CustomerCode, customer.FullName, customer.Phone), ct);

        return Result.Success(MapToDto(customer));
    }

    public async Task<Result<CustomerDto>> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Failure<CustomerDto>("Customer.ValidationFailed", validation.Errors[0].ErrorMessage);

        var customer = await store.GetByIdAsync(id, includeDetails: true, ct);
        if (customer is null)
            return Result.Failure<CustomerDto>("Customer.NotFound", "Client introuvable.");

        if (customer.RowVersion != request.RowVersion)
            return Result.Failure<CustomerDto>("Customer.ConcurrencyConflict", "Ce client a été modifié par un autre poste.");

        var before = new { customer.FullName, customer.Phone, customer.Email, customer.CustomerType };
        customer.CustomerType = request.CustomerType.ToDomain();
        customer.FullName = request.FullName.Trim();
        customer.NormalizedName = Customer.NormalizeText(request.FullName);
        customer.Phone = request.Phone.Trim();
        customer.NormalizedPhone = Customer.NormalizePhoneDigits(request.Phone);
        customer.WhatsAppNumber = string.IsNullOrWhiteSpace(request.WhatsAppNumber) ? null : request.WhatsAppNumber.Trim();
        customer.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        customer.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        customer.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        await unitOfWork.SaveChangesAsync(ct);
        await auditService.LogAsync("CustomerUpdated", nameof(Customer), customer.Id, before, new { customer.FullName, customer.Phone, customer.Email, customer.CustomerType }, ct);
        return Result.Success(MapToDto(customer));
    }

    public async Task<Result<CustomerDto>> UpdatePreferencesAsync(Guid id, UpdateContactPreferenceRequest request, CancellationToken ct = default)
    {
        var customer = await store.GetByIdAsync(id, includeDetails: true, ct);
        if (customer is null)
            return Result.Failure<CustomerDto>("Customer.NotFound", "Client introuvable.");

        var pref = customer.ContactPreference;
        pref.AllowWhatsApp = !request.UnsubscribedAll && request.AllowWhatsApp;
        pref.AllowSms = !request.UnsubscribedAll && request.AllowSms;
        pref.AllowEmail = !request.UnsubscribedAll && request.AllowEmail;
        pref.MarketingConsent = !request.UnsubscribedAll && request.MarketingConsent;
        pref.UnsubscribedAll = request.UnsubscribedAll;
        pref.PreferredLanguage = request.PreferredLanguage.ToDomain();
        pref.PrimaryChannel = request.PrimaryChannel.ToDomain();

        await unitOfWork.SaveChangesAsync(ct);
        await auditService.LogAsync("CustomerPreferencesUpdated", nameof(Customer), customer.Id, null, request, ct);
        return Result.Success(MapToDto(customer));
    }

    public async Task<Result<CustomerDto>> UpsertCompanyContractAsync(Guid id, UpsertCompanyContractRequest request, CancellationToken ct = default)
    {
        var validation = await contractValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Failure<CustomerDto>("Customer.ValidationFailed", validation.Errors[0].ErrorMessage);

        var customer = await store.GetByIdAsync(id, includeDetails: true, ct);
        if (customer is null)
            return Result.Failure<CustomerDto>("Customer.NotFound", "Client introuvable.");

        customer.CustomerType = DomainCustomerType.Corporate;
        customer.CompanyContract ??= new CompanyContract
        {
            BranchId = customer.BranchId,
            CustomerId = customer.Id
        };

        customer.CompanyContract.CompanyName = request.CompanyName.Trim();
        customer.CompanyContract.TaxId = request.TaxId?.Trim();
        customer.CompanyContract.RegistrationNumber = request.RegistrationNumber?.Trim();
        customer.CompanyContract.NegotiatedDiscountPercent = request.NegotiatedDiscountPercent;
        customer.CompanyContract.MonthlyGroupBilling = request.MonthlyGroupBilling;
        customer.CompanyContract.CreditLimitFcfa = request.CreditLimitFcfa;
        customer.CompanyContract.ContractStartDate = request.ContractStartDate;
        customer.CompanyContract.ContractEndDate = request.ContractEndDate;
        customer.CompanyContract.IsActive = request.IsActive;
        customer.CompanyContract.BillingContactName = request.BillingContactName?.Trim();
        customer.CompanyContract.BillingEmail = request.BillingEmail?.Trim();

        await unitOfWork.SaveChangesAsync(ct);
        await auditService.LogAsync("CompanyContractUpdated", nameof(CompanyContract), customer.CompanyContract.Id, null, request, ct);
        return Result.Success(MapToDto(customer));
    }

    public async Task<Result<CustomerDto>> MergeAsync(MergeCustomersRequest request, CancellationToken ct = default)
    {
        var validation = await mergeValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Failure<CustomerDto>("Customer.ValidationFailed", validation.Errors[0].ErrorMessage);

        var primary = await store.GetByIdAsync(request.PrimaryCustomerId, includeDetails: true, ct);
        var duplicate = await store.GetByIdAsync(request.DuplicateCustomerId, includeDetails: true, ct);
        if (primary is null || duplicate is null)
            return Result.Failure<CustomerDto>("Customer.NotFound", "Client source ou cible introuvable.");

        await store.ReassignOrdersToCustomerAsync(duplicate.Id, primary.Id, ct);

        primary.LoyaltyPoints += duplicate.LoyaltyPoints;
        primary.NyangoTokens += duplicate.NyangoTokens;
        if (string.IsNullOrWhiteSpace(primary.Email)) primary.Email = duplicate.Email;
        if (string.IsNullOrWhiteSpace(primary.Address)) primary.Address = duplicate.Address;
        if (string.IsNullOrWhiteSpace(primary.WhatsAppNumber)) primary.WhatsAppNumber = duplicate.WhatsAppNumber;

        duplicate.IsDeleted = true;
        duplicate.DeletedAt = DateTimeOffset.UtcNow;
        duplicate.MergedIntoCustomerId = primary.Id;

        await unitOfWork.SaveChangesAsync(ct);
        await auditService.LogAsync("CustomersMerged", nameof(Customer), primary.Id, new { DuplicateId = duplicate.Id }, new { PrimaryId = primary.Id }, ct);

        return Result.Success(MapToDto(primary));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await store.GetByIdAsync(id, includeDetails: true, ct);
        if (customer is null)
            return Result.Failure("Customer.NotFound", "Client introuvable.");

        customer.IsDeleted = true;
        customer.DeletedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);
        await auditService.LogAsync("CustomerDeleted", nameof(Customer), customer.Id, null, new { customer.CustomerCode }, ct);
        return Result.Success();
    }

    public static CustomerDto MapToDto(Customer c) => new(
        c.Id,
        c.CustomerCode,
        c.CustomerType.ToShared(),
        c.FullName,
        c.Phone,
        c.WhatsAppNumber,
        c.Email,
        c.Address,
        c.Notes,
        c.LoyaltyPoints,
        c.NyangoTokens,
        new ContactPreferenceDto(
            c.ContactPreference.AllowWhatsApp,
            c.ContactPreference.AllowSms,
            c.ContactPreference.AllowEmail,
            c.ContactPreference.MarketingConsent,
            c.ContactPreference.UnsubscribedAll,
            c.ContactPreference.PreferredLanguage.ToShared(),
            c.ContactPreference.PrimaryChannel.ToShared()),
        c.CompanyContract is null
            ? null
            : new CompanyContractDto(
                c.CompanyContract.Id,
                c.CompanyContract.CompanyName,
                c.CompanyContract.TaxId,
                c.CompanyContract.RegistrationNumber,
                c.CompanyContract.NegotiatedDiscountPercent,
                c.CompanyContract.MonthlyGroupBilling,
                c.CompanyContract.CreditLimitFcfa,
                c.CompanyContract.ContractStartDate,
                c.CompanyContract.ContractEndDate,
                c.CompanyContract.IsActive,
                c.CompanyContract.BillingContactName,
                c.CompanyContract.BillingEmail),
        c.CreatedAt,
        c.UpdatedAt,
        c.RowVersion);
}

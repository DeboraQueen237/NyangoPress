using Microsoft.AspNetCore.Mvc;
using NyangoPress.Api.Common;
using NyangoPress.Application.Customers;
using NyangoPress.Shared.Common;
using NyangoPress.Shared.Customers;

namespace NyangoPress.Api.Endpoints.Customers;

public static class CustomerEndpoints
{
    public static RouteGroupBuilder MapCustomerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/customers").WithTags("Customers");

        group.MapGet("/", async (
            [AsParameters] CustomerQueryParams query,
            ICustomerService service,
            CancellationToken ct) =>
        {
            var result = await service.SearchAsync(query, ct);
            return Results.Ok(result);
        }).RequireAuthorization(Policies.CustomersManage);

        group.MapGet("/{id:guid}", async (Guid id, ICustomerService service, CancellationToken ct)
            => (await service.GetByIdAsync(id, ct)).ToHttpResult())
            .RequireAuthorization(Policies.CustomersManage);

        group.MapGet("/{id:guid}/history", async (Guid id, ICustomerService service, CancellationToken ct)
            => (await service.GetHistoryAsync(id, ct)).ToHttpResult())
            .RequireAuthorization(Policies.CustomersManage);

        group.MapGet("/duplicates", async (
            [FromQuery] string? fullName,
            [FromQuery] string? phone,
            [FromQuery] Guid? excludeId,
            ICustomerService service,
            CancellationToken ct) =>
        {
            var matches = await service.CheckDuplicatesAsync(fullName ?? string.Empty, phone ?? string.Empty, excludeId, ct);
            return Results.Ok(matches);
        }).RequireAuthorization(Policies.CustomersManage);

        group.MapPost("/", async (CreateCustomerRequest request, ICustomerService service, CancellationToken ct) =>
        {
            var res = await service.CreateAsync(request, ct);
            return res.IsSuccess
                ? Results.Created($"/api/customers/{res.Value!.Id}", res.Value)
                : res.ToHttpResult();
        }).RequireAuthorization(Policies.CustomersManage);

        group.MapPut("/{id:guid}", async (Guid id, UpdateCustomerRequest request, ICustomerService service, CancellationToken ct)
            => (await service.UpdateAsync(id, request, ct)).ToHttpResult())
            .RequireAuthorization(Policies.CustomersManage);

        group.MapPut("/{id:guid}/preferences", async (Guid id, UpdateContactPreferenceRequest request, ICustomerService service, CancellationToken ct)
            => (await service.UpdatePreferencesAsync(id, request, ct)).ToHttpResult())
            .RequireAuthorization(Policies.CustomersManage);

        group.MapPut("/{id:guid}/contract", async (Guid id, UpsertCompanyContractRequest request, ICustomerService service, CancellationToken ct)
            => (await service.UpsertCompanyContractAsync(id, request, ct)).ToHttpResult())
            .RequireAuthorization(Policies.CustomersAdmin);

        group.MapPost("/merge", async (MergeCustomersRequest request, ICustomerService service, CancellationToken ct)
            => (await service.MergeAsync(request, ct)).ToHttpResult())
            .RequireAuthorization(Policies.CustomersAdmin);

        group.MapDelete("/{id:guid}", async (Guid id, ICustomerService service, CancellationToken ct)
            => (await service.DeleteAsync(id, ct)).ToHttpResult())
            .RequireAuthorization(Policies.CustomersAdmin);

        group.MapGet("/export/csv", async (ICustomerImportExportService io, CancellationToken ct) =>
        {
            var bytes = await io.ExportCsvAsync(ct);
            return Results.File(bytes, "text/csv; charset=utf-8", $"nyangopress-clients-{DateTime.UtcNow:yyyyMMdd}.csv");
        }).RequireAuthorization(Policies.CustomersAdmin);

        group.MapGet("/export/excel", async (ICustomerImportExportService io, CancellationToken ct) =>
        {
            var bytes = await io.ExportExcelAsync(ct);
            return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"nyangopress-clients-{DateTime.UtcNow:yyyyMMdd}.xlsx");
        }).RequireAuthorization(Policies.CustomersAdmin);

        group.MapGet("/import/template/csv", async (ICustomerImportExportService io, CancellationToken ct) =>
        {
            var bytes = await io.GetCsvTemplateAsync(ct);
            return Results.File(bytes, "text/csv; charset=utf-8", "modele-import-clients.csv");
        }).RequireAuthorization(Policies.CustomersAdmin);

        group.MapGet("/import/template/excel", async (ICustomerImportExportService io, CancellationToken ct) =>
        {
            var bytes = await io.GetExcelTemplateAsync(ct);
            return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "modele-import-clients.xlsx");
        }).RequireAuthorization(Policies.CustomersAdmin);

        group.MapPost("/import", async (IFormFile file, ICustomerImportExportService io, CancellationToken ct) =>
        {
            await using var stream = file.OpenReadStream();
            var result = await io.ImportAsync(stream, file.FileName, ct);
            return result.ToHttpResult();
        })
        .DisableAntiforgery()
        .RequireAuthorization(Policies.CustomersAdmin);

        return group;
    }
}

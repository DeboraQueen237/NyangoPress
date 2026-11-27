using NyangoPress.Api.Common;
using NyangoPress.Application.Common;
using NyangoPress.Application.Employees;

namespace NyangoPress.Api.Endpoints.Employees;

public static class EmployeeEndpoints
{
    public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/employees")
            .WithTags("Employés")
            .RequireAuthorization(Policies.ManageEmployees);

        group.MapGet("/", async ([AsParameters] EmployeeQuery query, EmployeeService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(query, ct)));

        group.MapGet("/{id:guid}", async (Guid id, EmployeeService service, CancellationToken ct) =>
            (await service.GetAsync(id, ct)).ToHttpResult());

        group.MapPost("/", async Task<IResult> (CreateEmployeeRequest request, EmployeeService service, IConfiguration config, CancellationToken ct) =>
            {
                var result = await service.CreateAsync(request, ct);
                if (result.IsFailure)
                {
                    return result.Error.ToProblem();
                }

                var created = result.Value;
                var expose = config.GetValue<bool>("Account:ExposeTemporaryPassword");
                var body = new CreateEmployeeResponse(created.Employee, expose ? created.TemporaryPassword : null);
                return Results.Created($"/api/employees/{created.Employee.Id}", body);
            })
            .WithValidation<CreateEmployeeRequest>();

        group.MapPut("/{id:guid}", async (Guid id, UpdateEmployeeRequest request, EmployeeService service, CancellationToken ct) =>
                (await service.UpdateAsync(id, request, ct)).ToHttpResult())
            .WithValidation<UpdateEmployeeRequest>();

        group.MapPost("/{id:guid}/activate", async (Guid id, EmployeeService service, CancellationToken ct) =>
            (await service.SetActiveAsync(id, true, ct)).ToHttpResult());

        group.MapPost("/{id:guid}/deactivate", async (Guid id, EmployeeService service, CancellationToken ct) =>
            (await service.SetActiveAsync(id, false, ct)).ToHttpResult());

        group.MapGet("/{id:guid}/photo", async Task<IResult> (Guid id, ProfileService profile, CancellationToken ct) =>
        {
            var result = await profile.OpenPhotoAsync(id, ct);
            return result.IsFailure
                ? result.Error.ToProblem()
                : Results.Stream(result.Value.Content, result.Value.ContentType);
        });

        return app;
    }
}

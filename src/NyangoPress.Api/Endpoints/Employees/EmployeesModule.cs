using NyangoPress.Api.Modules;
using NyangoPress.Application.Employees;

namespace NyangoPress.Api.Endpoints.Employees;

/// <summary>Employés et profil (Dev 1). Les validateurs sont déjà enregistrés par IdentityModule (toute l'assembly Application).</summary>
public sealed class EmployeesModule : IModule
{
    public int Order => 20;

    public void AddServices(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<EmployeeService>();
        builder.Services.AddScoped<ProfileService>();
    }

    public void MapEndpoints(WebApplication app)
    {
        app.MapEmployeeEndpoints();
        app.MapProfileEndpoints();
    }
}

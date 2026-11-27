using NyangoPress.Api.Middleware;
using NyangoPress.Api.Modules;
using NyangoPress.Api.Startup;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/nyangopress-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30));

// Tous les modules (classes IModule) sont découverts automatiquement : ne modifiez PAS ce fichier pour ajouter le vôtre.
var modules = ModuleCatalog.Discover();
foreach (var module in modules)
{
    module.AddServices(builder);
}

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

foreach (var module in modules)
{
    module.MapEndpoints(app);
}

await DatabaseInitializer.InitializeAsync(app);

app.Run();

// Nécessaire pour que les tests d'intégration (WebApplicationFactory) puissent démarrer l'API.
public partial class Program
{
}

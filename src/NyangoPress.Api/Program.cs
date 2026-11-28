using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using NyangoPress.Api.Common;
using NyangoPress.Api.Endpoints.Auctions;
using NyangoPress.Api.Endpoints.Customers;
using NyangoPress.Api.Endpoints.Delivery;
using NyangoPress.Api.Endpoints.Orders;
using NyangoPress.Api.Endpoints.Public;
using NyangoPress.Api.Hubs;
using NyangoPress.Application.Common;
using NyangoPress.Infrastructure;
using NyangoPress.Infrastructure.Persistence;
using NyangoPress.Infrastructure.Seed;
using NyangoPress.Shared.Common;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

var uploadsPath = Path.Combine(AppContext.BaseDirectory, "AppData", "uploads");
Directory.CreateDirectory(uploadsPath);

builder.Services.AddNyangoPressApplication();
builder.Services.AddNyangoPressInfrastructure((sp, options) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var pgConnection = cfg.GetConnectionString("Default");
    var dbProvider = cfg["Database:Provider"];
    var sqliteConnection = cfg.GetConnectionString("Sqlite")
        ?? $"Data Source={Path.Combine(AppContext.BaseDirectory, "AppData", "nyangopress.db")}";

    if (string.Equals(dbProvider, "PostgreSQL", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(pgConnection))
    {
        options.UseNpgsql(pgConnection);
    }
    else
    {
        options.UseSqlite(sqliteConnection);
    }
}, uploadsPath);

builder.Services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    jwtKey = JwtAuthHelper.DefaultSigningKey;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "NyangoPress",
            ValidAudience = "NyangoPress.Clients",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    ctx.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(Policies.StaffOnly, p => p.RequireRole(Roles.AllStaff));
    options.AddPolicy(Policies.CustomersManage, p => p.RequireRole(Roles.Admin, Roles.Manager, Roles.Cashier));
    options.AddPolicy(Policies.CustomersAdmin, p => p.RequireRole(Roles.Admin, Roles.Manager));
    options.AddPolicy(Policies.OrdersCreate, p => p.RequireRole(Roles.Admin, Roles.Manager, Roles.Cashier));
    options.AddPolicy(Policies.OrdersCancel, p => p.RequireRole(Roles.Admin, Roles.Manager, Roles.Cashier));
    options.AddPolicy(Policies.OrdersOverrideBalance, p => p.RequireRole(Roles.Admin, Roles.Manager));
    options.AddPolicy(Policies.ProductionAdvance, p => p.RequireRole(Roles.Admin, Roles.Manager, Roles.Cashier, Roles.Operator));
    options.AddPolicy(Policies.DeliveryManage, p => p.RequireRole(Roles.Admin, Roles.Manager, Roles.Cashier));
    options.AddPolicy(Policies.DeliveryZones, p => p.RequireRole(Roles.Admin, Roles.Manager));
    options.AddPolicy(Policies.DeliveryDriver, p => p.RequireRole(Roles.Admin, Roles.Manager, Roles.Driver));
    options.AddPolicy(Policies.AuctionsManage, p => p.RequireRole(Roles.Admin, Roles.Manager));
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("PublicTracking", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 60;
        limiter.QueueLimit = 5;
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(_ => true)
              .AllowCredentials());
});

builder.Services.AddHealthChecks();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL" &&
        db.Database.GetMigrations().Any())
    {
        await db.Database.MigrateAsync();
    }
    else
    {
        await db.Database.EnsureCreatedAsync();
    }

    if (!app.Environment.IsEnvironment("Testing"))
    {
        await Dev2SeedData.SeedAsync(db);
    }
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseRateLimiter();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapAuthEndpoints(app.Configuration);
app.MapCustomerEndpoints();
app.MapOrdersModuleEndpoints();
app.MapDeliveryEndpoints();
app.MapPublicTrackingEndpoints();
app.MapAuctionEndpoints();

app.MapHub<OrdersHub>("/hubs/orders");
app.MapHub<DeliveryHub>("/hubs/delivery").AllowAnonymous();

app.Run();

public partial class Program;

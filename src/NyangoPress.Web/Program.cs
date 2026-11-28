using MudBlazor.Services;
using NyangoPress.Web.Components;
using NyangoPress.Web.Localization;
using NyangoPress.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5059";
builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
});

builder.Services.AddScoped<AuthSessionService>();
builder.Services.AddSingleton<LanguageService>();
builder.Services.AddScoped<ApiClient>();
builder.Services.AddScoped<SignalRService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

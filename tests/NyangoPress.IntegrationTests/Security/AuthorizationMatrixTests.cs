using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NyangoPress.Application.Common;
using NyangoPress.Application.Identity;
using NyangoPress.Domain.Common;
using NyangoPress.Domain.Identity;

namespace NyangoPress.IntegrationTests.Security;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseEnvironment("Testing");
}

/// <summary>
/// Teste TOUS les endpoints de l'API (y compris ceux des autres modules) : sans jeton -> 401 ;
/// avec un rôle non autorisé par la politique de l'endpoint -> 403 ; mot de passe à changer -> 403.
/// Aucune base de données n'est nécessaire : l'autorisation est refusée avant l'exécution de l'endpoint.
/// </summary>
public sealed partial class AuthorizationMatrixTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string SampleId = "00000000-0000-0000-0000-000000000001";

    private sealed record EndpointInfo(string Method, string Path, string? Policy);

    private List<EndpointInfo> ProtectedEndpoints()
    {
        var source = factory.Services.GetRequiredService<EndpointDataSource>();
        var list = new List<EndpointInfo>();

        foreach (var endpoint in source.Endpoints.OfType<RouteEndpoint>())
        {
            if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            {
                continue;
            }

            var method = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.FirstOrDefault();
            if (method is null)
            {
                continue;
            }

            var path = PlaceholderPattern().Replace(endpoint.RoutePattern.RawText ?? string.Empty, SampleId);
            var policy = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
                .Select(a => a.Policy)
                .FirstOrDefault(p => !string.IsNullOrEmpty(p));

            list.Add(new EndpointInfo(method, path, policy));
        }

        return list;
    }

    private string TokenFor(string role, bool mustChangePassword = false)
    {
        using var scope = factory.Services.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var subject = new TokenSubject(Guid.NewGuid(), role.ToLowerInvariant(), role, role, DefaultBranch.Id, mustChangePassword);
        return tokens.CreateAccessToken(subject).Token;
    }

    private static HttpRequestMessage Request(EndpointInfo endpoint, string? token)
    {
        var request = new HttpRequestMessage(new HttpMethod(endpoint.Method), endpoint.Path);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }

    [Fact]
    public void Every_endpoint_uses_a_known_policy()
    {
        var unknown = ProtectedEndpoints()
            .Where(e => e.Policy is not null && !Policies.RolesByPolicy.ContainsKey(e.Policy))
            .Select(e => $"{e.Method} {e.Path} -> {e.Policy}")
            .ToList();

        Assert.True(unknown.Count == 0, "Politiques inconnues (à ajouter dans Policies.cs par PR) : " + string.Join(" ; ", unknown));
    }

    [Fact]
    public async Task Anonymous_requests_get_401()
    {
        var client = factory.CreateClient();
        var failures = new List<string>();

        foreach (var endpoint in ProtectedEndpoints())
        {
            using var response = await client.SendAsync(Request(endpoint, null));
            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                failures.Add($"{endpoint.Method} {endpoint.Path} -> {(int)response.StatusCode}");
            }
        }

        Assert.True(failures.Count == 0, "Attendu 401 : " + string.Join(" ; ", failures));
    }

    [Fact]
    public async Task Roles_not_allowed_by_the_policy_get_403()
    {
        var client = factory.CreateClient();
        var failures = new List<string>();

        foreach (var endpoint in ProtectedEndpoints().Where(e => e.Policy is not null && Policies.RolesByPolicy.ContainsKey(e.Policy)))
        {
            var allowed = Policies.RolesByPolicy[endpoint.Policy!];
            foreach (var role in Roles.All.Where(r => !allowed.Contains(r)))
            {
                using var response = await client.SendAsync(Request(endpoint, TokenFor(role)));
                if (response.StatusCode != HttpStatusCode.Forbidden)
                {
                    failures.Add($"{role} sur {endpoint.Method} {endpoint.Path} -> {(int)response.StatusCode}");
                }
            }
        }

        Assert.True(failures.Count == 0, "Attendu 403 : " + string.Join(" ; ", failures));
    }

    [Fact]
    public async Task Users_who_must_change_their_password_are_blocked_everywhere_else()
    {
        var client = factory.CreateClient();
        var failures = new List<string>();

        foreach (var endpoint in ProtectedEndpoints().Where(e => e.Policy != Policies.AuthenticatedAny))
        {
            using var response = await client.SendAsync(Request(endpoint, TokenFor(Roles.Admin, mustChangePassword: true)));
            if (response.StatusCode != HttpStatusCode.Forbidden)
            {
                failures.Add($"{endpoint.Method} {endpoint.Path} -> {(int)response.StatusCode}");
            }
        }

        Assert.True(failures.Count == 0, "Attendu 403 : " + string.Join(" ; ", failures));
    }

    [GeneratedRegex(@"\{[^}]+\}")]
    private static partial Regex PlaceholderPattern();
}

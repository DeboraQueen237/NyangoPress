using System.Security.Claims;
using NyangoPress.Application.Common;
using NyangoPress.Domain.Common;

namespace NyangoPress.Api.Common;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId => Guid.TryParse(Principal?.FindFirst("sub")?.Value, out var id) ? id : null;

    public string? UserName => Principal?.FindFirst("name")?.Value;

    public string? Role => Principal?.FindFirst("role")?.Value;

    public Guid BranchId => Guid.TryParse(Principal?.FindFirst("branch")?.Value, out var id) ? id : DefaultBranch.Id;

    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;
}

using System.Security.Claims;
using NyangoPress.Application.Common;
using NyangoPress.Domain.Common;

namespace NyangoPress.Api.Common;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var raw = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string DisplayName
        => User?.FindFirstValue(ClaimTypes.Name) ?? "Système";

    public string? Role
        => User?.FindFirstValue(ClaimTypes.Role);

    public Guid BranchId
    {
        get
        {
            var raw = User?.FindFirstValue("branch_id");
            return Guid.TryParse(raw, out var id) ? id : BaseEntity.DefaultBranchId;
        }
    }

    public bool IsInRole(string role)
        => User?.IsInRole(role) == true || string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);
}

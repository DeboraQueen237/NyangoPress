namespace NyangoPress.Application.Common;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string DisplayName { get; }
    string? Role { get; }
    Guid BranchId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}

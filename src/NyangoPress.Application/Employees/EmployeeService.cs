using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NyangoPress.Application.Common;
using NyangoPress.Application.Common.Events;
using NyangoPress.Application.Identity;
using NyangoPress.Domain.Identity;

namespace NyangoPress.Application.Employees;

/// <summary>
/// Gestion des employés (EMP-01 à EMP-02, EMP-04). Règles : seul un Admin crée ou modifie un Admin ;
/// on ne peut ni se désactiver ni changer son propre rôle ; désactiver ou changer de rôle coupe les sessions.
/// </summary>
public sealed class EmployeeService(
    UserManager<AppUser> userManager,
    IAppDbContext db,
    ICurrentUser currentUser,
    IAuditService audit,
    IDomainEventDispatcher events,
    AuthService auth)
{
    private static readonly Error NotFound = Error.NotFound("Employee.NotFound", "Employé introuvable.");

    public async Task<PagedResult<EmployeeListItem>> ListAsync(EmployeeQuery q, CancellationToken ct)
    {
        var (page, size) = PagedResult<EmployeeListItem>.Normalize(q.Page, q.PageSize);
        var branch = currentUser.BranchId;
        var staff = Roles.Staff.ToList();

        var query =
            from u in db.Set<AppUser>().AsNoTracking()
            join ur in db.Set<IdentityUserRole<Guid>>() on u.Id equals ur.UserId
            join r in db.Set<AppRole>() on ur.RoleId equals r.Id
            where u.BranchId == branch && staff.Contains(r.Name!)
            select new
            {
                u.Id,
                UserName = u.UserName!,
                u.FullName,
                Email = u.Email!,
                u.PhoneNumber,
                Role = r.Name!,
                u.IsActive,
                u.LastLoginAt,
            };

        if (!string.IsNullOrWhiteSpace(q.Role))
        {
            var role = q.Role;
            query = query.Where(x => x.Role == role);
        }

        if (q.IsActive is { } active)
        {
            query = query.Where(x => x.IsActive == active);
        }

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            s = (s.Length > 100 ? s[..100] : s).ToLower();
            query = query.Where(x => x.FullName.ToLower().Contains(s)
                                     || x.UserName.ToLower().Contains(s)
                                     || x.Email.ToLower().Contains(s));
        }

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderBy(x => x.FullName)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        var items = rows
            .Select(x => new EmployeeListItem(x.Id, x.UserName, x.FullName, x.Email, x.PhoneNumber, x.Role, x.IsActive, x.LastLoginAt))
            .ToList();

        return new PagedResult<EmployeeListItem>(items, page, size, total);
    }

    public async Task<Result<EmployeeDetails>> GetAsync(Guid id, CancellationToken ct)
    {
        var user = await FindAsync(id, ct);
        var role = user is null ? null : await StaffRoleOfAsync(user);
        return user is null || role is null ? NotFound : ToDetails(user, role);
    }

    public async Task<Result<CreatedEmployee>> CreateAsync(CreateEmployeeRequest r, CancellationToken ct)
    {
        if (!Roles.Staff.Contains(r.Role))
        {
            return Error.Validation("Employee.InvalidRole", "Rôle invalide.");
        }

        if (Protect(r.Role) is { } denied)
        {
            return denied;
        }

        var userName = r.UserName.Trim();
        var email = r.Email.Trim();

        if (await userManager.FindByNameAsync(userName) is not null)
        {
            return Error.Conflict("Employee.UserNameTaken", "Cet identifiant existe déjà.");
        }

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return Error.Conflict("Employee.EmailTaken", "Cet e-mail est déjà utilisé.");
        }

        var password = PasswordGenerator.Generate();
        var user = new AppUser
        {
            Id = Guid.CreateVersion7(),
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            FullName = r.FullName.Trim(),
            PhoneNumber = Clean(r.PhoneNumber),
            IsActive = true,
            MustChangePassword = true,
            BranchId = currentUser.BranchId,
        };

        var created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            return Error.Validation("Employee.CreateFailed", Describe(created));
        }

        var inRole = await userManager.AddToRoleAsync(user, r.Role);
        if (!inRole.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return Error.Failure("Employee.RoleFailed", Describe(inRole));
        }

        var profile = new EmployeeProfile
        {
            UserId = user.Id,
            WhatsAppNumber = Clean(r.WhatsAppNumber),
            Address = Clean(r.Address),
            Language = r.Language,
            HireDate = r.HireDate,
        };
        db.Set<EmployeeProfile>().Add(profile);
        await db.SaveChangesAsync(ct);
        user.Profile ??= profile;

        await audit.LogAsync(
            "Employee.Created",
            nameof(AppUser),
            user.Id.ToString(),
            newValues: new { user.UserName, user.FullName, user.Email, Role = r.Role },
            ct: ct);

        await events.DispatchAsync(
            new UserCreated
            {
                UserId = user.Id,
                UserName = userName,
                FullName = user.FullName,
                Email = email,
                PhoneNumber = user.PhoneNumber ?? profile.WhatsAppNumber,
                Language = r.Language,
                TemporaryPassword = password,
            },
            ct);

        return new CreatedEmployee(ToDetails(user, r.Role), password);
    }

    public async Task<Result<EmployeeDetails>> UpdateAsync(Guid id, UpdateEmployeeRequest r, CancellationToken ct)
    {
        var user = await FindAsync(id, ct);
        var currentRole = user is null ? null : await StaffRoleOfAsync(user);
        if (user is null || currentRole is null)
        {
            return NotFound;
        }

        if (Protect(currentRole) is { } denied)
        {
            return denied;
        }

        var newRole = r.Role;
        if (!Roles.Staff.Contains(newRole))
        {
            return Error.Validation("Employee.InvalidRole", "Rôle invalide.");
        }

        if (Protect(newRole) is { } denied2)
        {
            return denied2;
        }

        var roleChanged = newRole != currentRole;
        if (roleChanged && user.Id == currentUser.UserId)
        {
            return Error.Forbidden("Employee.CannotChangeOwnRole", "Vous ne pouvez pas changer votre propre rôle.");
        }

        var email = r.Email.Trim();
        if (!string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            var other = await userManager.FindByEmailAsync(email);
            if (other is not null && other.Id != user.Id)
            {
                return Error.Conflict("Employee.EmailTaken", "Cet e-mail est déjà utilisé.");
            }
        }

        var before = new { user.FullName, user.Email, user.PhoneNumber, Role = currentRole, user.Profile?.WhatsAppNumber, user.Profile?.Address };

        user.FullName = r.FullName.Trim();
        user.Email = email;
        user.NormalizedEmail = userManager.NormalizeEmail(email);
        user.PhoneNumber = Clean(r.PhoneNumber);

        var profile = user.Profile;
        if (profile is null)
        {
            profile = new EmployeeProfile { UserId = user.Id };
            db.Set<EmployeeProfile>().Add(profile);
            user.Profile = profile;
        }

        profile.WhatsAppNumber = Clean(r.WhatsAppNumber);
        profile.Address = Clean(r.Address);
        profile.HireDate = r.HireDate;
        profile.Notes = Clean(r.Notes);

        var updated = await userManager.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            return Error.Failure("Employee.UpdateFailed", Describe(updated));
        }

        if (roleChanged)
        {
            var removed = await userManager.RemoveFromRoleAsync(user, currentRole);
            var added = removed.Succeeded ? await userManager.AddToRoleAsync(user, newRole) : removed;
            if (!added.Succeeded)
            {
                return Error.Failure("Employee.RoleFailed", Describe(added));
            }

            await auth.RevokeAllSessionsAsync(user.Id, "RoleChanged", ct);
            await audit.LogAsync(
                "Employee.RoleChanged",
                nameof(AppUser),
                user.Id.ToString(),
                oldValues: new { Role = currentRole },
                newValues: new { Role = newRole },
                ct: ct);
        }

        await audit.LogAsync(
            "Employee.Updated",
            nameof(AppUser),
            user.Id.ToString(),
            oldValues: before,
            newValues: new { user.FullName, user.Email, user.PhoneNumber, Role = newRole, profile.WhatsAppNumber, profile.Address },
            ct: ct);

        return ToDetails(user, newRole);
    }

    public async Task<Result> SetActiveAsync(Guid id, bool active, CancellationToken ct)
    {
        var user = await FindAsync(id, ct);
        var role = user is null ? null : await StaffRoleOfAsync(user);
        if (user is null || role is null)
        {
            return NotFound;
        }

        if (!active && user.Id == currentUser.UserId)
        {
            return Error.Forbidden("Employee.CannotDeactivateSelf", "Vous ne pouvez pas désactiver votre propre compte.");
        }

        if (Protect(role) is { } denied)
        {
            return denied;
        }

        if (user.IsActive == active)
        {
            return Result.Success();
        }

        user.IsActive = active;
        if (active)
        {
            await userManager.SetLockoutEndDateAsync(user, null);
            await userManager.ResetAccessFailedCountAsync(user);
        }

        var updated = await userManager.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            return Error.Failure("Employee.UpdateFailed", Describe(updated));
        }

        if (!active)
        {
            await auth.RevokeAllSessionsAsync(user.Id, "Deactivated", ct);
        }

        await audit.LogAsync(
            active ? "Employee.Activated" : "Employee.Deactivated",
            nameof(AppUser),
            user.Id.ToString(),
            ct: ct);

        return Result.Success();
    }

    private Task<AppUser?> FindAsync(Guid id, CancellationToken ct)
    {
        var branch = currentUser.BranchId;
        return userManager.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == id && u.BranchId == branch, ct);
    }

    private async Task<string?> StaffRoleOfAsync(AppUser user) =>
        (await userManager.GetRolesAsync(user)).FirstOrDefault(r => Roles.Staff.Contains(r));

    /// <summary>Un Manager ne peut ni créer, ni modifier, ni désactiver un Administrateur.</summary>
    private Error? Protect(string role) =>
        role == Roles.Admin && !currentUser.IsInRole(Roles.Admin)
            ? Error.Forbidden("Employee.AdminProtected", "Seul un administrateur peut gérer un compte administrateur.")
            : null;

    private static EmployeeDetails ToDetails(AppUser u, string role) => new(
        u.Id,
        u.UserName ?? string.Empty,
        u.FullName,
        u.Email ?? string.Empty,
        u.PhoneNumber,
        role,
        u.IsActive,
        u.MustChangePassword,
        u.LastLoginAt,
        u.Profile?.WhatsAppNumber,
        u.Profile?.Address,
        u.Profile?.Language ?? "fr",
        u.Profile?.HireDate,
        u.Profile?.Notes,
        !string.IsNullOrEmpty(u.Profile?.PhotoPath),
        u.CreatedAt);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Describe(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(e => e.Description));
}

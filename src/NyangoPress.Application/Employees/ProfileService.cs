using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NyangoPress.Application.Common;
using NyangoPress.Domain.Identity;

namespace NyangoPress.Application.Employees;

/// <summary>Profil personnel (EMP-03) : coordonnées, langue, thème, photo.</summary>
public sealed class ProfileService(
    UserManager<AppUser> userManager,
    IAppDbContext db,
    IFileStorage storage,
    ICurrentUser currentUser)
{
    private static readonly Error NotFound = Error.NotFound("Profile.NotFound", "Profil introuvable.");

    public async Task<Result<ProfileDto>> GetAsync(Guid userId, CancellationToken ct)
    {
        var user = await FindAsync(userId, ct);
        return user is null ? NotFound : await ToDtoAsync(user);
    }

    public async Task<Result<ProfileDto>> UpdateAsync(Guid userId, UpdateProfileRequest r, CancellationToken ct)
    {
        var user = await FindAsync(userId, ct);
        if (user is null)
        {
            return NotFound;
        }

        user.FullName = r.FullName.Trim();
        user.PhoneNumber = Clean(r.PhoneNumber);

        var profile = EnsureProfile(user);
        profile.WhatsAppNumber = Clean(r.WhatsAppNumber);
        profile.Address = Clean(r.Address);
        profile.Language = r.Language;
        profile.Theme = r.Theme;

        var updated = await userManager.UpdateAsync(user);
        return updated.Succeeded
            ? await ToDtoAsync(user)
            : Error.Failure("Profile.UpdateFailed", string.Join(" ", updated.Errors.Select(e => e.Description)));
    }

    public async Task<Result> SetPhotoAsync(Guid userId, Stream content, string contentType, CancellationToken ct)
    {
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return Error.Validation("Profile.PhotoMustBeImage", "La photo doit être une image (JPEG, PNG ou WebP).");
        }

        var user = await FindAsync(userId, ct);
        if (user is null)
        {
            return NotFound;
        }

        var saved = await storage.SaveAsync(content, contentType, "photos/profils", ct);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        var profile = EnsureProfile(user);
        var oldPath = profile.PhotoPath;
        profile.PhotoPath = saved.Value;
        await db.SaveChangesAsync(ct);

        if (!string.IsNullOrEmpty(oldPath))
        {
            await storage.DeleteAsync(oldPath, ct);
        }

        return Result.Success();
    }

    public async Task<Result<PhotoFile>> OpenPhotoAsync(Guid userId, CancellationToken ct)
    {
        var branch = currentUser.BranchId;
        var path = await db.Set<EmployeeProfile>()
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.BranchId == branch)
            .Select(p => p.PhotoPath)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrEmpty(path))
        {
            return Error.NotFound("Profile.NoPhoto", "Aucune photo.");
        }

        var stream = await storage.OpenReadAsync(path, ct);
        if (stream is null)
        {
            return Error.NotFound("Profile.NoPhoto", "Aucune photo.");
        }

        var contentType = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg",
        };

        return new PhotoFile(stream, contentType);
    }

    private Task<AppUser?> FindAsync(Guid userId, CancellationToken ct) =>
        userManager.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct);

    private EmployeeProfile EnsureProfile(AppUser user)
    {
        if (user.Profile is not null)
        {
            return user.Profile;
        }

        var profile = new EmployeeProfile { UserId = user.Id };
        db.Set<EmployeeProfile>().Add(profile);
        user.Profile = profile;
        return profile;
    }

    private async Task<ProfileDto> ToDtoAsync(AppUser u)
    {
        var role = (await userManager.GetRolesAsync(u)).FirstOrDefault() ?? string.Empty;
        return new ProfileDto(
            u.Id,
            u.UserName ?? string.Empty,
            u.FullName,
            u.Email ?? string.Empty,
            u.PhoneNumber,
            role,
            u.Profile?.WhatsAppNumber,
            u.Profile?.Address,
            u.Profile?.Language ?? "fr",
            u.Profile?.Theme ?? "system",
            u.Profile?.HireDate,
            !string.IsNullOrEmpty(u.Profile?.PhotoPath));
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

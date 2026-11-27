using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NyangoPress.Application.Common;
using NyangoPress.Application.Identity;
using NyangoPress.Domain.Identity;
using NyangoPress.Infrastructure.Identity;
using NyangoPress.Infrastructure.Persistence;

namespace NyangoPress.UnitTests.Identity;

public class IdentityTests
{
    private const string StrongPassword = "Lavage-Express-2026";

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddNyangoIdentity();
        return services.BuildServiceProvider();
    }

    private static AppUser NewUser(string userName) => new()
    {
        Id = Guid.NewGuid(),
        UserName = userName,
        Email = userName + "@example.com",
        FullName = "Test",
    };

    [Theory]
    [InlineData("Court1A")]
    [InlineData("toutenminuscule1234")]
    [InlineData("TOUTENMAJUSCULE1234")]
    [InlineData("SansAucunChiffreIci")]
    [InlineData("Password1234")]
    public async Task Weak_passwords_are_rejected(string password)
    {
        using var sp = BuildServices();
        var manager = sp.GetRequiredService<UserManager<AppUser>>();

        var result = await manager.CreateAsync(NewUser("alice"), password);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Strong_password_is_accepted()
    {
        using var sp = BuildServices();
        var manager = sp.GetRequiredService<UserManager<AppUser>>();

        var result = await manager.CreateAsync(NewUser("bob"), StrongPassword);

        Assert.True(result.Succeeded, string.Join(" ", result.Errors.Select(e => e.Description)));
    }

    [Fact]
    public async Task Password_containing_the_user_name_is_rejected()
    {
        using var sp = BuildServices();
        var manager = sp.GetRequiredService<UserManager<AppUser>>();

        var result = await manager.CreateAsync(NewUser("marie.dupont"), "Marie.Dupont-2026");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Generated_temporary_passwords_respect_the_policy()
    {
        using var sp = BuildServices();
        var manager = sp.GetRequiredService<UserManager<AppUser>>();
        var user = NewUser("carol");

        for (var i = 0; i < 100; i++)
        {
            var password = PasswordGenerator.Generate();
            foreach (var validator in manager.PasswordValidators)
            {
                var result = await validator.ValidateAsync(manager, user, password);
                Assert.True(result.Succeeded, $"{password} : {string.Join(" ", result.Errors.Select(e => e.Description))}");
            }
        }
    }

    [Fact]
    public async Task Account_is_locked_after_five_failed_attempts()
    {
        using var sp = BuildServices();
        var manager = sp.GetRequiredService<UserManager<AppUser>>();
        var user = NewUser("dave");
        await manager.CreateAsync(user, StrongPassword);

        for (var i = 0; i < 4; i++)
        {
            await manager.AccessFailedAsync(user);
        }

        Assert.False(await manager.IsLockedOutAsync(user));

        await manager.AccessFailedAsync(user);

        Assert.True(await manager.IsLockedOutAsync(user));
    }

    [Fact]
    public async Task Successful_login_resets_the_failure_counter()
    {
        using var sp = BuildServices();
        var manager = sp.GetRequiredService<UserManager<AppUser>>();
        var user = NewUser("erin");
        await manager.CreateAsync(user, StrongPassword);

        for (var i = 0; i < 3; i++)
        {
            await manager.AccessFailedAsync(user);
        }

        await manager.ResetAccessFailedCountAsync(user);

        for (var i = 0; i < 3; i++)
        {
            await manager.AccessFailedAsync(user);
        }

        Assert.False(await manager.IsLockedOutAsync(user));
    }

    [Fact]
    public void Only_administrators_access_admin_only_policy()
    {
        var roles = Policies.RolesByPolicy[Policies.AdminOnly];

        Assert.Single(roles);
        Assert.Equal(Roles.Admin, roles[0]);
    }

    [Fact]
    public void Cashiers_and_drivers_cannot_manage_employees_or_catalog()
    {
        Assert.DoesNotContain(Roles.Cashier, Policies.RolesByPolicy[Policies.ManageEmployees]);
        Assert.DoesNotContain(Roles.Driver, Policies.RolesByPolicy[Policies.ManageEmployees]);
        Assert.DoesNotContain(Roles.Cashier, Policies.RolesByPolicy[Policies.ManageCatalog]);
    }

    [Fact]
    public void Every_policy_only_references_known_roles()
    {
        foreach (var (policy, roles) in Policies.RolesByPolicy)
        {
            foreach (var role in roles)
            {
                Assert.True(Roles.All.Contains(role), $"{policy} référence le rôle inconnu {role}");
            }
        }
    }
}

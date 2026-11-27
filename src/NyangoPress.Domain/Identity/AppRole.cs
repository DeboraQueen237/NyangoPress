using Microsoft.AspNetCore.Identity;

namespace NyangoPress.Domain.Identity;

public class AppRole : IdentityRole<Guid>
{
    public AppRole()
    {
    }

    public AppRole(string name)
        : base(name)
    {
        Id = Guid.CreateVersion7();
    }

    public string? Description { get; set; }
}

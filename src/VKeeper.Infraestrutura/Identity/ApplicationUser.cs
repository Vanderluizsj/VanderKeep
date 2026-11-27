using Microsoft.AspNetCore.Identity;

namespace VKeeper.Infraestrutura.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
}

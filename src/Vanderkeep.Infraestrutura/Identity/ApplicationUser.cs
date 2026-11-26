using Microsoft.AspNetCore.Identity;

namespace Vanderkeep.Infraestrutura.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
}

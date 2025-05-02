using Microsoft.AspNetCore.Identity;

namespace BuzzWatch.Infrastructure.Identity
{
    public class AppUser : IdentityUser<Guid>
    {
        // Additional properties for user management
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? LastLoginAt { get; set; }
    }
} 
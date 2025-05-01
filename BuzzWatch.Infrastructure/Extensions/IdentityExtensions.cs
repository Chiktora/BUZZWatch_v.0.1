using BuzzWatch.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace BuzzWatch.Infrastructure.Extensions
{
    public static class IdentityExtensions
    {
        public static async Task SeedIdentityAsync(this IServiceProvider sp)
        {
            using var scope = sp.CreateScope();
            var um = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var rm = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            foreach (var role in new[] { "Admin", "Moderator", "User" })
                if (!await rm.RoleExistsAsync(role))
                    await rm.CreateAsync(new IdentityRole<Guid>(role));

            var admin = await um.FindByEmailAsync("admin@local");
            if (admin == null)
            {
                admin = new AppUser { UserName = "admin@local", Email = "admin@local" };
                await um.CreateAsync(admin, "Pa$$w0rd!");
                await um.AddToRoleAsync(admin, "Admin");
            }
        }
    }
} 
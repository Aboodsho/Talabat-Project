using Core.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
namespace Infrastructure.Identity
{
    public static class IdentitySeeder
    {
        public static async Task SeedRoleAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var roleManger=scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            foreach (var roleName in new[] { Role.Admin, Role.User })
            { 
                if (!await roleManger.RoleExistsAsync(roleName))
                {
                    await roleManger.CreateAsync(new IdentityRole<Guid> { Id=Guid.NewGuid(), Name= roleName });
                }
            }
        }

        /////
        ///


        public static async Task SeedAdminAsync( IServiceProvider services, IConfiguration configuration)
            {
                var email = configuration["Admin:Email"];
                var password = configuration["Admin:Password"];

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                    return; 

                using var scope = services.CreateScope();
                var userManager = scope.ServiceProvider
                    .GetRequiredService<UserManager<ApplicationUser>>();

                if (await userManager.FindByEmailAsync(email) is not null)
                    return; 

                var admin = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = email,
                    Email = email,
                    FullName = "Administrator",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, password);
                if (!result.Succeeded)
                    throw new InvalidOperationException(
                        string.Join("; ", result.Errors.Select(e => e.Description)));

                await userManager.AddToRoleAsync(admin, Role.Admin);
            }
}
}

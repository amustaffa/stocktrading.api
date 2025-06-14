// Utility class for seeding initial data (roles, admin user).
using Microsoft.AspNetCore.Identity;
using StockTrading.Models.Domain;
namespace StockTrading.Data;        
public class SeedData
{
    public async Task Initialize(IServiceProvider serviceProvider,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<SeedData>>();

        // Seed Roles
        string[] roleNames = { "Admin", "User" };
        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
                logger.LogInformation("Role '{RoleName}' created.", roleName);
            }
        }

        // Seed Admin User
        var adminUserEmail = "admin@example.com";
        var adminPassword = "AdminPassword123!"; // Change this in production!
        if (await userManager.FindByEmailAsync(adminUserEmail) == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminUserEmail,
                Email = adminUserEmail,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Admin"
            };
            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                logger.LogInformation("Admin user '{Email}' created and assigned 'Admin' role.", adminUserEmail);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogError("Failed to create admin user '{Email}': {Errors}", adminUserEmail, errors);
            }
        }
    }
}

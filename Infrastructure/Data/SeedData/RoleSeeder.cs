using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Enums; // for AccountStatus enum
using Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data.SeedData
{
    public static class RoleSeeder
    {
        public static async Task SeedRolesAndAdminsAsync(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("RoleSeeder");

            // 1️⃣ Seed Roles
            string[] roles = { "Admin", "Organizer", "Customer" };

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole<int>(roleName));
                    logger.LogInformation("Created role: {Role}", roleName);
                }
            }

            // 2️⃣ Seed Admin Users
            var adminUsers = configuration.GetSection("AdminUsers").Get<List<AdminSeedModel>>() ?? new();

            foreach (var admin in adminUsers)
            {
                if (string.IsNullOrWhiteSpace(admin.Email) || string.IsNullOrWhiteSpace(admin.Password))
                {
                    logger.LogWarning("Skipping admin seed entry with missing Email/Password.");
                    continue;
                }

                var existingUser = await userManager.FindByEmailAsync(admin.Email);

                if (existingUser == null)
                {
                    var user = new User
                    {
                        UserName = admin.UserName ?? admin.Email,
                        Email = admin.Email,
                        FirstName = "System",
                        LastName = "Admin",
                        Role = "Admin",
                        AccountStatus = AccountStatus.Active,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var createResult = await userManager.CreateAsync(user, admin.Password);
                    if (!createResult.Succeeded)
                    {
                        logger.LogError("❌ Failed to create admin {Email}: {Errors}",
                            admin.Email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                        continue;
                    }

                    logger.LogInformation("✅ Created admin user: {Email}", admin.Email);
                    existingUser = user;
                }

                // Ensure user has correct info and role
                bool needsUpdate = false;

                if (existingUser.Role != "Admin")
                {
                    existingUser.Role = "Admin";
                    needsUpdate = true;
                }

                if (existingUser.AccountStatus != AccountStatus.Active)
                {
                    existingUser.AccountStatus = AccountStatus.Active;
                    needsUpdate = true;
                }

                if (string.IsNullOrWhiteSpace(existingUser.FirstName))
                {
                    existingUser.FirstName = "System";
                    needsUpdate = true;
                }

                if (string.IsNullOrWhiteSpace(existingUser.LastName))
                {
                    existingUser.LastName = "Admin";
                    needsUpdate = true;
                }

                if (needsUpdate)
                    await userManager.UpdateAsync(existingUser);

                if (!await userManager.IsInRoleAsync(existingUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(existingUser, "Admin");
                    logger.LogInformation("Assigned Admin role to {Email}", admin.Email);
                }
            }
        }
    }

    public class AdminSeedModel
    {
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Identity;

namespace IdentityServer.Data
{
    public class SeedData
    {
        public static void Migrate(IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var applicationContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                applicationContext.Database.Migrate();
            }
        }

        public static async Task InitializeDatabase(IApplicationBuilder app, IConfiguration configuration)
        {
            using (var scope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

                foreach (var role in GetSystemRoles())
                {
                    var existingEntity = roleManager.FindByNameAsync(role.Name).Result;

                    if (existingEntity == null)
                    {
                        await roleManager.CreateAsync(role);
                    }
                }

                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                foreach (var pair in GetTestSystemUsersWithRoles())
                {
                    var user = await userManager.FindByEmailAsync(pair.Key.Email);
                    if (user == null)
                    {
                        await userManager.CreateAsync(pair.Key, "password");
                        user = await userManager.FindByEmailAsync(pair.Key.Email);
                        await userManager.AddToRoleAsync(user, pair.Value);
                    }
                    else
                    {
                        await userManager.UpdateAsync(user);
                    }
                }
            }
        }

        public static IEnumerable<ApplicationRole> GetSystemRoles()
        {
            return new List<ApplicationRole>() {
                new ApplicationRole(){
                    Name = SystemRoles.AdminRoleName
                },
                new ApplicationRole(){
                    Name = SystemRoles.ProjectManagerRoleName
                },
                new ApplicationRole(){
                    Name = SystemRoles.ModeratorRoleName
                },
                new ApplicationRole(){
                    Name = SystemRoles.ClientRoleName
                },
            };
        }

        public static Dictionary<ApplicationUser, string> GetTestSystemUsersWithRoles()
        {
            var clientUser = new ApplicationUser()
            {
                Email = "client@test.com",
                UserName = "client@test.com",
                FirstName = "Test",
                LastName = "Client",
                BirthDate = DateTime.Now,
                EnglishLevel = Shared.Common.EnglishLevel.Beginner,
                JobTitle = "Dotnet",
                Departament = ".Net"
            };

            var moderatorUser = new ApplicationUser()
            {
                Email = "moderator@test.com",
                UserName = "moderator@test.com",
                FirstName = "Test",
                LastName = "Moderator",
                BirthDate = DateTime.Now,
                EnglishLevel = Shared.Common.EnglishLevel.Beginner,
                JobTitle = "Dotnet",
                Departament = ".Net"
            };

            var projectManagerUser = new ApplicationUser()
            {
                Email = "manager@test.com",
                UserName = "manager@test.com",
                FirstName = "Test",
                LastName = "ProjectManager",
                BirthDate = DateTime.Now,
                EnglishLevel = Shared.Common.EnglishLevel.Beginner,
                JobTitle = "Dotnet",
                Departament = ".Net"
            };

            var adminUser = new ApplicationUser()
            {
                Email = "admin@test.com",
                UserName = "admin@test.com",
                FirstName = "Test",
                LastName = "Admin",
                BirthDate = DateTime.Now,
                EnglishLevel = Shared.Common.EnglishLevel.Beginner,
                JobTitle = "Dotnet",
                Departament = ".Net"
            };

            return new Dictionary<ApplicationUser, string>()
            {
                {
                    clientUser, SystemRoles.ClientRoleName
                },
                {
                    moderatorUser, SystemRoles.ModeratorRoleName
                },
                {
                    projectManagerUser, SystemRoles.ProjectManagerRoleName
                },
                {
                    adminUser, SystemRoles.AdminRoleName
                },
            };
        }

    }
}

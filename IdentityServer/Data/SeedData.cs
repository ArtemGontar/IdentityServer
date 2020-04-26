﻿using System.Collections.Generic;
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

        public static Dictionary<ApplicationUser, string> GetTestSystemUsersWithRoles()
        {
            var clientUser = new ApplicationUser()
            {
                Email = "cleint@test.com",
                UserName = "cleint@test.com"
            };

            
            return new Dictionary<ApplicationUser, string>()
            {
                {
                    clientUser, "Client"
                },
            };
        }

    }
}

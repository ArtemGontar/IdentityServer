using IdentityModel;
using IdentityServer4.Models;
using Shared.Identity;
using System.Collections.Generic;

namespace IdentityServer
{
    public static class Config
    {
        public static IEnumerable<ApiResource> GetApiResources()
        {
            return new List<ApiResource>()
            {
                new ApiResource("QuizApi", "Quiz API"),
                new ApiResource("UserApi", "User API"),
                new ApiResource("StatisticApi", "Statistic API")
            };
        }

        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>()
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResource
                {
                    Name = "role",
                    UserClaims =
                    {
                        "role"
                    }
                },
                new IdentityResource
                {
                    Name = "userId",
                    UserClaims =
                    {
                        "userId"
                    }
                }
            };
        }

        public static IEnumerable<Client> GetClients()
        {
            return new List<Client>()
            {
                new Client {
                    RequireConsent = false,
                    ClientId = "angular_spa",
                    ClientName = "Angular SPA",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    AllowedScopes =
                    {
                        IdentityServer4.IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServer4.IdentityServerConstants.StandardScopes.Profile,
                        "role",
                        "userId",
                        "QuizApi",
                        "UserApi",
                        "StatisticApi"
                    },
                    AlwaysIncludeUserClaimsInIdToken = true,
                    RedirectUris = {
                        "http://localhost:4200/auth-callback"
                    },
                    PostLogoutRedirectUris = 
                    {
                        "http://localhost:4200"
                    },
                    AllowedCorsOrigins = {
                        "http://localhost:4200"
                    },
                    AllowAccessTokensViaBrowser = true,
                    AccessTokenLifetime = 3600
                }
            };
        }
    }
}

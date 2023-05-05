using Duende.IdentityServer.Models;

namespace IdentityServer;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[] {new IdentityResources.OpenId(), new IdentityResources.Profile(),};

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[] {new("api", "API", new[] {"profile", "name", "email", "role"})};

    public static IEnumerable<ApiResource> GetApiResource()
    {
        return new List<ApiResource> {new("api", "API") {Scopes = {"api"}},};
    }

    public static IEnumerable<Client> Clients =>
        new Client[]
        {
            // SwaggerUI client
            new Client
            {
                ClientId = "swagger-ui",
                ClientName = "Swagger UI",
                ClientSecrets = {new Secret("49C1A7E1-0C79-4A89-A3D6-A37998FB86B0".Sha256())},
                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                RequireClientSecret = false,
                AccessTokenLifetime = 14400,
                RedirectUris =
                {
                    "https://localhost:7249/swagger/oauth2-redirect.html"
                },
                AllowedCorsOrigins = {"https://localhost:7249"},
                AllowedScopes = {"openid", "profile", "api", "email"},
            },
        };
}

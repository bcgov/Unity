using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Security.Claims;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Unity.ApplicantPortal.Web.Services;

namespace Unity.ApplicantPortal.Web.Extensions;

public static class StartupConfigurations
{    
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        // This extension method implements IHttpClientFactory
        builder.Services.AddHttpClient<GrantManagerClient>(
            client =>
            {
                client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("App:SelfUrl") ?? string.Empty);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("unity-applicant-portal");
            }
            )
            .AddKeycloakAuthToken()
            .AddStandardResilienceHandler();
    }

    public static void AddAuthenticationServices(this IHostApplicationBuilder builder)
    {
        //Authentication Schems
        builder.Services.AddAuthentication(options =>
        {
            //Sets cookie authentication scheme
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(cookie =>
        {
            cookie.AccessDeniedPath = "/";
            cookie.LogoutPath = "/";
            //Sets the cookie name and maxage, so the cookie is invalidated.
            cookie.Cookie.Name = "keycloak.cookie";
            cookie.Cookie.MaxAge = TimeSpan.FromMinutes(600);
            cookie.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            cookie.SlidingExpiration = true;
        })
        .AddOpenIdConnect(options =>
        {
            options.Authority = $"{builder.Configuration.GetSection(Consts.AuthConfigSection)["auth-server-url"]}/realms/{builder.Configuration.GetSection(Consts.AuthConfigSection)["realm"]}";
            options.ClientId = builder.Configuration.GetSection(Consts.AuthConfigSection)["resource"];
            options.ClientSecret = builder.Configuration.GetSection(Consts.AuthConfigSection).GetSection("credentials")["secret"];
            options.MetadataAddress = $"{builder.Configuration.GetSection(Consts.AuthConfigSection)["auth-server-url"]}/realms/{builder.Configuration.GetSection(Consts.AuthConfigSection)["realm"]}/.well-known/openid-configuration";

            options.RequireHttpsMetadata = false;
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.MapInboundClaims = false;

            // SameSite is needed for Chrome/Firefox, as they will give http error 500 back, if not set to unspecified.
            options.NonceCookie.SameSite = SameSiteMode.Unspecified;
            options.CorrelationCookie.SameSite = SameSiteMode.Unspecified;

            options.Events.OnRedirectToIdentityProvider = async n =>
            {
                n.ProtocolMessage.RedirectUri = options.CallbackPath + "/signin-oidc";
                await Task.FromResult(0);
            };

            options.Scope.Add("openid");
            options.Scope.Add("profile");

            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = "name",
                RoleClaimType = ClaimTypes.Role,
                ValidateIssuer = true,
            };
        });
    }
}

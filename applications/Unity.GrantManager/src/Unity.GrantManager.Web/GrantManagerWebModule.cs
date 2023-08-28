using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Unity.GrantManager.EntityFrameworkCore;
using Unity.GrantManager.Localization;
using Unity.GrantManager.MultiTenancy;
using Unity.GrantManager.Web.Menus;
using Microsoft.OpenApi.Models;
using Volo.Abp;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Identity.Web;
using Volo.Abp.Modularity;
using Volo.Abp.SettingManagement.Web;
using Volo.Abp.Swashbuckle;
using Volo.Abp.TenantManagement.Web;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.UI.Navigation;
using Volo.Abp.VirtualFileSystem;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Basic;
using Volo.Abp.AspNetCore.Mvc.UI.Theming;
using System;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Threading.Tasks;
using Unity.GrantManager.Web.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Logging;
using Unity.GrantManager.Web.Identity.Policy;
using Microsoft.AspNetCore.Http;
using Volo.Abp.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.CookiePolicy;

namespace Unity.GrantManager.Web;

[DependsOn(
    typeof(GrantManagerHttpApiModule),
    typeof(GrantManagerApplicationModule),
    typeof(GrantManagerEntityFrameworkCoreModule),
    typeof(AbpAutofacModule),
    typeof(AbpIdentityWebModule),
    typeof(AbpSettingManagementWebModule),    
    typeof(AbpAspNetCoreMvcUiBasicThemeModule),
    typeof(AbpTenantManagementWebModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpSwashbuckleModule),
    typeof(AbpAccountWebOpenIddictModule),
    typeof(AbpAspNetCoreAuthenticationOpenIdConnectModule)
)]
public class GrantManagerWebModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.PreConfigure<AbpMvcDataAnnotationsLocalizationOptions>(options =>
        {
            options.AddAssemblyResource(
                typeof(GrantManagerResource),
                typeof(GrantManagerDomainModule).Assembly,
                typeof(GrantManagerDomainSharedModule).Assembly,
                typeof(GrantManagerApplicationModule).Assembly,
                typeof(GrantManagerApplicationContractsModule).Assembly,
                typeof(GrantManagerWebModule).Assembly
            );
        });

        PreConfigure<OpenIddictBuilder>(builder =>
        {
            builder.AddValidation(options =>
            {
                options.AddAudiences("GrantManager");
                options.UseLocalServer();
                options.UseAspNetCore();
            });
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        var configuration = context.Services.GetConfiguration();

        ConfigurePolicies(context);
        ConfigureAuthentication(context, configuration);
        ConfigureUrls(configuration);
        ConfigureTheming(configuration);
        ConfigureBundles(configuration);
        ConfigureAutoMapper();
        ConfigureVirtualFileSystem(hostingEnvironment);
        ConfigureNavigationServices();
        ConfigureAutoApiControllers();
        ConfigureSwaggerServices(context.Services);
        ConfigureAccessTokenManagement(context, configuration);
    }

    private static void ConfigurePolicies(ServiceConfigurationContext context)
    {
        PolicyRegistrant.Register(context);
    }

    private static void ConfigureAuthentication(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Events.OnSigningOut = async e =>
            {
                // revoke refresh token on sign-out
                await e.HttpContext.RevokeUserRefreshTokenAsync();
            };
        })
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.Authority = configuration["AuthServer:ServerAddress"] + "/realms/" + configuration["AuthServer:Realm"];
            options.RequireHttpsMetadata = Convert.ToBoolean(configuration["AuthServer:RequireHttpsMetadata"]);
            options.ResponseType = OpenIdConnectResponseType.Code;

            options.ClientId = configuration["AuthServer:ClientId"];
            options.ClientSecret = configuration["AuthServer:ClientSecret"];

            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;

            options.ClaimActions.MapClaimTypes();
            options.TokenValidationParameters = new TokenValidationParameters
            {
                RoleClaimType = UnityClaimsTypes.Role
            };

            options.Events.OnTokenResponseReceived = async (tokenReceivedContext) =>
            {
                await Task.CompletedTask;
            };

            options.Events.OnTokenValidated = async (tokenValidatedContext) =>
            {
                var updater = tokenValidatedContext.HttpContext.RequestServices.GetService<IdentityProfileLoginUpdater>();
                await updater!.UpdateAsync(tokenValidatedContext);
            };

            if (Convert.ToBoolean(configuration["AuthServer:IsBehindTlsTerminationProxy"]))
            {
                // Rewrite OIDC redirect URI on OpenShift (Staging, Production) environments or if requested
                options.Events.OnRedirectToIdentityProvider = context =>
                {
                    var host = context.Request.Host;
                    context.ProtocolMessage.SetParameter("redirect_uri", $"https://{host}/signin-oidc");

                    if (!string.IsNullOrEmpty(configuration["AuthServer:IdpHint"]))
                    {
                        context.ProtocolMessage.SetParameter(configuration["AuthServer:IdpHintKey"], configuration["AuthServer:IdpHint"]);
                    }

                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToIdentityProviderForSignOut = ctx =>
                {
                    var host = ctx.Request.Host;
                    ctx.ProtocolMessage.SetParameter("post_logout_redirect_uri", $"https://{host}/signout-callback-oidc");

                    return Task.CompletedTask;
                };
            }
            else
            {
                //// Change OIDC cookie policies when developing locally
                // Allows http://localhost to work on Chromium and Edge.
                options.ProtocolValidator.RequireNonce = false;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.CorrelationCookie.SameSite = SameSiteMode.Unspecified;
            }
        });
    }

    private static void ConfigureAccessTokenManagement(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddAccessTokenManagement(options =>
        {
            // client config is inferred from OpenID Connect settings
            // if you want to specify scopes explicitly, do it here, otherwise the scope parameter will not be sent
            //options.Client.Scope = "api";
        })
        .ConfigureBackchannelHttpClient();

        // Configure transient retry policy for access token
        //.AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(new[]
        //{
        //    TimeSpan.FromSeconds(1),
        //    TimeSpan.FromSeconds(2),
        //    TimeSpan.FromSeconds(3)
        //}));

        // registers HTTP client that uses the managed user access token
        context.Services.AddUserAccessTokenHttpClient("user_client", configureClient: client =>
        {
            client.BaseAddress = new Uri(configuration["AuthServer:ServerAddress"] + "/realms/" + configuration["AuthServer:Realm"]);
        });

        // registers HTTP client that uses the managed client access token
        context.Services.AddClientAccessTokenHttpClient("client", configureClient: client =>
        {
            client.BaseAddress = new Uri(configuration["AuthServer:ServerAddress"] + "/realms/" + configuration["AuthServer:Realm"]);
        });
    }

    private void ConfigureUrls(IConfiguration configuration)
    {
        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];
        });
    }

    private void ConfigureTheming(IConfiguration configuration)
    {
        Configure<AbpThemingOptions>(options =>
        {
            options.DefaultThemeName = configuration["Theme:Name"];
        });
    }

    private void ConfigureBundles(IConfiguration configuration)
    {
        Configure<AbpBundlingOptions>(options =>
        {
            options.StyleBundles.Configure(
                string.Concat(configuration["Theme:Name"], ".Global"),
                bundle =>
                {
                    bundle.AddFiles("/global-styles.css");
                }
            );
        });
    }

    private void ConfigureAutoMapper()
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<GrantManagerWebModule>();
        });
    }

    private void ConfigureVirtualFileSystem(IWebHostEnvironment hostingEnvironment)
    {
        if (!hostingEnvironment.IsProduction())
        {
            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                // TODO: Not used but raises error in container: System.IO.DirectoryNotFoundException: /Unity.GrantManager.Domain.Shared/
                // options.FileSets.ReplaceEmbeddedByPhysical<GrantManagerDomainSharedModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}Unity.GrantManager.Domain.Shared"));
                options.FileSets.ReplaceEmbeddedByPhysical<GrantManagerDomainModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}Unity.GrantManager.Domain"));
                options.FileSets.ReplaceEmbeddedByPhysical<GrantManagerApplicationContractsModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}Unity.GrantManager.Application.Contracts"));
                options.FileSets.ReplaceEmbeddedByPhysical<GrantManagerApplicationModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}Unity.GrantManager.Application"));
                options.FileSets.ReplaceEmbeddedByPhysical<GrantManagerWebModule>(hostingEnvironment.ContentRootPath);
            });
        }
    }

    private void ConfigureNavigationServices()
    {
        Configure<AbpNavigationOptions>(options =>
        {
            options.MenuContributors.Add(new GrantManagerMenuContributor());
        });
    }

    private void ConfigureAutoApiControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(GrantManagerApplicationModule).Assembly);
        });
    }

    private void ConfigureSwaggerServices(IServiceCollection services)
    {
        services.AddAbpSwaggerGen(
            options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "GrantManager API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
            }
        );
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();
        var configuration = context.GetConfiguration();

        if (!env.IsProduction())
        {
            app.UseDeveloperExceptionPage();
            IdentityModelEventSource.ShowPII = true;
        }

        app.UseAbpRequestLocalization();

        if (env.IsProduction())
        {
            app.UseErrorPage();
        }

        if (Convert.ToBoolean(configuration["AuthServer:IsBehindTlsTerminationProxy"]))
        {
            app.UseCookiePolicy(new CookiePolicyOptions
            {
                HttpOnly = HttpOnlyPolicy.Always,
                Secure = CookieSecurePolicy.Always,
                MinimumSameSitePolicy = SameSiteMode.None
            });
        }

        app.UseCorrelationId();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();

        if (MultiTenancyConsts.IsEnabled)
        {
            app.UseMultiTenancy();
        }

        app.UseUnitOfWork();
        app.UseAuthorization();
        app.UseSwagger();
        app.UseAbpSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "GrantManager API");
        });
        app.UseAuditing();
        app.UseAbpSerilogEnrichers();
        app.UseConfiguredEndpoints();
    }
}

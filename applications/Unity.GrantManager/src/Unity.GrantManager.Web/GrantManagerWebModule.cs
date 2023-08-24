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
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite.Bundling;
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
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Basic.Bundling;
using System;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Volo.Abp.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Threading.Tasks;
using Unity.GrantManager.Web.Identity;
using Microsoft.IdentityModel.Tokens;
using Volo.Abp.Identity;
using Microsoft.IdentityModel.Logging;

namespace Unity.GrantManager.Web;

[DependsOn(
    typeof(GrantManagerHttpApiModule),
    typeof(GrantManagerApplicationModule),
    typeof(GrantManagerEntityFrameworkCoreModule),
    typeof(AbpAutofacModule),
    typeof(AbpIdentityWebModule),
    typeof(AbpSettingManagementWebModule),
    typeof(AbpAspNetCoreMvcUiLeptonXLiteThemeModule),
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
        ConfigureAuthorization(context);
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

            options.Events.OnTokenResponseReceived = async (context) =>
            {
                await Task.CompletedTask;
            };

            options.Events.OnTokenValidated = async (context) =>
            {
                var updater = context.HttpContext.RequestServices.GetService<IdentityProfileLoginUpdater>();

                // TODO: can be used to create users locally
                await updater!.UpdateAsync(context);
            };
        });
    }

    private static void ConfigureAuthorization(ServiceConfigurationContext context)
    {
        // TODO: ABP maps these, figure out how to map from database
        // Configure your policies
        context.Services.AddAuthorization(options =>
              options.AddPolicy(IdentityPermissions.UserLookup.Default,
              policy => policy.RequireClaim("Permission", IdentityPermissions.UserLookup.Default)));
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
        if (hostingEnvironment.IsDevelopment())
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

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            IdentityModelEventSource.ShowPII = true;
        }

        app.UseAbpRequestLocalization();

        if (!env.IsDevelopment())
        {
            app.UseErrorPage();
        }

        app.UseCorrelationId();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        //app.UseAbpOpenIddictValidation();

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

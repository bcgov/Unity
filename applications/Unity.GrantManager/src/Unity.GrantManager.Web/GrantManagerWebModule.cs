using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Controllers.Auth.FormSubmission;
using Unity.GrantManager.Controllers.Authentication.FormSubmission;
using Unity.GrantManager.Controllers.Authentication.FormSubmission.FormIdResolvers;
using Unity.GrantManager.EntityFrameworkCore;
using Unity.GrantManager.HealthChecks;
using Unity.GrantManager.Localization;
using Unity.GrantManager.MultiTenancy;
using Unity.GrantManager.Web.Exceptions;
using Unity.GrantManager.Web.Filters;
using Unity.GrantManager.Web.Identity;
using Unity.GrantManager.Web.Identity.Policy;
using Unity.GrantManager.Web.Menus;
using Unity.GrantManager.Web.Services;
using Unity.Identity.Web;
using Unity.TenantManagement.Web;
using Volo.Abp;
using Volo.Abp.AspNetCore.Auditing;
using Volo.Abp.AspNetCore.Authentication.OpenIdConnect;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.AntiForgery;
using Volo.Abp.AspNetCore.Mvc.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Auditing;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.BlobStoring;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.SecurityLog;
using Volo.Abp.SettingManagement.Web;
using Volo.Abp.Swashbuckle;
using Volo.Abp.Timing;
using Volo.Abp.UI.Navigation;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.VirtualFileSystem;
using Unity.Notifications.Web;
using Unity.Payments.Web;
using Unity.Payments;
using Unity.AspNetCore.Mvc.UI.Theme.UX2;
using Unity.AspNetCore.Mvc.UI.Theme.UX2.Bundling;
using Unity.Flex.Web;

namespace Unity.GrantManager.Web;

[DependsOn(
    typeof(GrantManagerHttpApiModule),
    typeof(GrantManagerApplicationModule),
    typeof(GrantManagerEntityFrameworkCoreModule),
    typeof(AbpAutofacModule),
    typeof(AbpSettingManagementWebModule),
    typeof(UnityAspNetCoreMvcUIThemeUX2Module),
    typeof(UnityTenantManagementWebModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpSwashbuckleModule),
    typeof(AbpAspNetCoreAuthenticationOpenIdConnectModule),
    typeof(UnitydentityWebModule),
    typeof(AbpBlobStoringModule),
    typeof(PaymentsWebModule),
    typeof(AbpBlobStoringModule),
    typeof(NotificationsWebModule),
    typeof(FlexWebModule)
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

        Configure<TokenCleanupOptions>(options =>
        {
            options.IsCleanupEnabled = false; // not used
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        var configuration = context.Services.GetConfiguration();

        ConfgureFormsApiAuhentication(context);
        ConfigureAuthentication(context, configuration);
        ConfigurePolicies(context);
        ConfigureUrls(configuration);
        ConfigureBundles();
        ConfigureAutoMapper();
        ConfigureVirtualFileSystem(hostingEnvironment);
        ConfigureNavigationServices();
        ConfigureAutoApiControllers();
        ConfigureSwaggerServices(context.Services);
        ConfigureAccessTokenManagement(context, configuration);
        ConfigureUtils(context);

        Configure<AbpBackgroundJobOptions>(options =>
        {
            options.IsJobExecutionEnabled = true;
        });

        Configure<AbpBackgroundWorkerQuartzOptions>(options =>
        {
            options.IsAutoRegisterEnabled = configuration.GetValue<bool>("BackgroundJobs:Quartz:IsAutoRegisterEnabled");
        });

        Configure<AbpAntiForgeryOptions>(options =>
        {
            options.TokenCookie.Expiration = TimeSpan.FromDays(365);
            options.TokenCookie.SecurePolicy = CookieSecurePolicy.Always;
            options.TokenCookie.SameSite = SameSiteMode.Lax;
            options.TokenCookie.HttpOnly = false;
        });
        Configure<AbpClockOptions>(options =>
        {
            options.Kind = DateTimeKind.Utc;
        });

        Configure<AbpAuditingOptions>(options =>
        {
            options.EntityHistorySelectors.Add(
                new NamedTypeSelector(
                 "ExplictEntityAudit",
                 type =>
                 {

                     if (type.Name.Contains("Role", StringComparison.OrdinalIgnoreCase)
                        || type.Name.Contains("User", StringComparison.OrdinalIgnoreCase)
                        || type.Name.Contains("Permission", StringComparison.OrdinalIgnoreCase))
                     {
                         return true;
                     }
                     else
                     {
                         return false;
                     }
                 }
                )
            );
        });

        Configure<AbpSecurityLogOptions>(x =>
        {
            x.ApplicationName = "GrantManager";
        });

        Configure<AbpAspNetCoreAuditingOptions>(options =>
        {
            options.IgnoredUrls.AddIfNotContains("/healthz");
        });

        context.Services.AddHealthChecks()
            .AddCheck<LiveHealthCheck>("live", tags: new[] { "live" });

        context.Services.AddHealthChecks()
           .AddCheck<ReadyHealthCheck>("ready", tags: new[] { "ready" });

        context.Services.AddHealthChecks()
           .AddCheck<StartupHealthCheck>("startup", tags: new[] { "startup" });
    }

    private static void ConfigureUtils(ServiceConfigurationContext context)
    {
        context.Services.AddScoped<BrowserUtils>();
    }

    private static void ConfgureFormsApiAuhentication(ServiceConfigurationContext context)
    {
        context.Services.AddScoped<FormsApiTokenAuthFilter>();
        context.Services.AddScoped<IFormIdResolver, FormIdHeadersResolver>();
        context.Services.AddScoped<IFormIdResolver, FormIdQueryStringResolver>();
        context.Services.AddScoped<IFormIdResolver, FormIdRequestBodyResolver>();
        context.Services.AddScoped<IFormIdResolver, FormIdRouteResolver>();
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
                try
                {
                    var loginHandler = tokenValidatedContext.HttpContext.RequestServices.GetService<IdentityProfileLoginHandler>();
                    await loginHandler!.HandleAsync(tokenValidatedContext);
                }
                catch (NoGrantProgramsLinkedException)
                {
                    // Extend this to more custom handling if this is extended
                    tokenValidatedContext.HandleResponse();

                    await tokenValidatedContext.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    await tokenValidatedContext.HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);

                    tokenValidatedContext.Response.Redirect("Account/NoGrantPrograms");
                }
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
        })
        .ConfigureBackchannelHttpClient();

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

    private void ConfigureBundles()
    {
        Configure<AbpBundlingOptions>(options =>
        {
            options
                .StyleBundles
                .Configure(UnityThemeUX2Bundles.Styles.Global, bundle =>
                {
                    bundle.AddFiles("/global-styles.css");
                });
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
            options.ConventionalControllers.Create(typeof(PaymentsApplicationModule).Assembly);
        });
    }

    private static void ConfigureSwaggerServices(IServiceCollection services)
    {
        services.AddAbpSwaggerGen(
            options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "GrantManager API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
                options.OperationFilter<ApiTokenAuthorizationHeaderParameter>();
                options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                {
                    Name = AuthConstants.ApiKeyHeader,
                    Description = "Authorization by x-api-key inside request's header",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "ApiKeyScheme"
                });
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
                MinimumSameSitePolicy = SameSiteMode.None,
                OnAppendCookie = cookieContext =>
                {
                    if (cookieContext.CookieName.Equals("XSRF-TOKEN"))
                    {
                        cookieContext.CookieOptions.HttpOnly = false;
                        cookieContext.CookieOptions.SameSite = SameSiteMode.Lax;
                    }
                }
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

        var supportedCultures = new[]
        {
            new CultureInfo("en-CA")
        };
        app.UseAbpRequestLocalization(options =>
        {
            options.DefaultRequestCulture = new RequestCulture("en-CA");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
        });
    }
}

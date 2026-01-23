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
using StackExchange.Profiling;
using StackExchange.Profiling.Storage;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Unity.AspNetCore.Mvc.UI.Theme.UX2;
using Unity.AspNetCore.Mvc.UI.Theme.UX2.Bundling;
using Unity.Flex.Web;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Controllers.Auth.FormSubmission;
using Unity.GrantManager.Controllers.Authentication.FormSubmission;
using Unity.GrantManager.Controllers.Authentication.FormSubmission.FormIdResolvers;
using Unity.GrantManager.EntityFrameworkCore;
using Unity.GrantManager.HealthChecks;
using Unity.GrantManager.Localization;
using Unity.GrantManager.MultiTenancy;
using Unity.GrantManager.Web.Components.MiniProfiler;
using Unity.GrantManager.Web.Exceptions;
using Unity.GrantManager.Web.Filters;
using Unity.GrantManager.Web.Identity;
using Unity.GrantManager.Web.Identity.Policy;
using Unity.GrantManager.Web.Menus;
using Unity.GrantManager.Web.Settings;
using Unity.Identity.Web;
using Unity.Notifications.Web;
using Unity.Payments;
using Unity.Payments.Web;
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
using Volo.Abp.BlobStoring;
using Volo.Abp.Identity;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.SecurityLog;
using Volo.Abp.SettingManagement.Web;
using Volo.Abp.SettingManagement.Web.Pages.SettingManagement;
using Volo.Abp.Swashbuckle;
using Volo.Abp.Timing;
using Volo.Abp.Ui.LayoutHooks;
using Volo.Abp.UI.Navigation;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.Users;
using Volo.Abp.VirtualFileSystem;
using StackExchange.Redis;
using Microsoft.AspNetCore.DataProtection;
using Unity.Modules.Shared.Utils;
using Unity.Notifications.Web.Bundling;
using Unity.Reporting.Web;
using Unity.GrantManager.Web.Views.Settings;

namespace Unity.GrantManager.Web;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(GrantManagerHttpApiModule),
    typeof(GrantManagerApplicationModule),
    typeof(GrantManagerEntityFrameworkCoreModule),
    typeof(AbpLocalizationModule),
    typeof(AbpIdentityDomainModule),
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
    typeof(FlexWebModule),
    typeof(ReportingWebModule)
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

    private static readonly string[] _liveHealthCheckTags = ["live"];
    private static readonly string[] _readyHealthCheckTags = ["ready"];
    private static readonly string[] _startupHealthCheckTags = ["startup"];

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
        ConfigureUtils(context);
        ConfigureDataProtection(context, configuration);
        ConfigureMiniProfiler(context, configuration);

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
                    "Abp.FullAuditedEntities",
                    type => typeof(IFullAuditedObject).IsAssignableFrom(type)
                )
            );

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

        Configure<SettingManagementPageOptions>(options =>
        {
            options.Contributors.Add(new BackgroundJobsPageContributor());
            options.Contributors.Add(new TagManagementPageContributor());
        });

        context.Services.AddHealthChecks()
            .AddCheck<LiveHealthCheck>("live", tags: _liveHealthCheckTags);

        context.Services.AddHealthChecks()
            .AddCheck<ReadyHealthCheck>("ready", tags: _readyHealthCheckTags);

        context.Services.AddHealthChecks()
            .AddCheck<StartupHealthCheck>("startup", tags: _startupHealthCheckTags);

        Configure<SettingManagementPageOptions>(options =>
        {
            options.Contributors.Add(new ApplicationUiSettingPageContributor());
        });
    }

    private static void ConfigureDataProtection(ServiceConfigurationContext context, IConfiguration configuration)
    {
        /* The rest of the Redis Configuration happens in the application layer */
        if (!Convert.ToBoolean(configuration["DataProtection:IsEnabled"])) return;
        if (!Convert.ToBoolean(configuration["Redis:IsEnabled"])) return;

        // Configure Data Protection
        if (Convert.ToBoolean(configuration["DataProtection:IsEnabled"]) && Convert.ToBoolean(configuration["Redis:IsEnabled"]))
        {
            context.Services.AddDataProtection()
            .SetApplicationName("UnityGrantManagerWeb")
            .PersistKeysToStackExchangeRedis(
                () =>
                {
                    var multiplexer = context.Services.GetRequiredService<IConnectionMultiplexer>();
                    return multiplexer.GetDatabase();
                },
               "Unity-DataKeys");

            context.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(8);
            });
        }
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
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = false;
            options.Events.OnSigningOut = async e => { await Task.CompletedTask; };
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
            options.MaxAge = TimeSpan.FromHours(8);

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
            options.Events.OnRedirectToIdentityProviderForSignOut = context =>
            {
                // Redirect to the IdP's logout endpoint
                var postLogoutRedirectUri = new Uri($"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}").AbsoluteUri;
                var idpLogoutUrl = $"{context.Options.Authority}/protocol/openid-connect/logout";

                var uri = new UriBuilder(idpLogoutUrl)
                {
                    Query = $"client_id={context.Options.ClientId}&post_logout_redirect_uri={Uri.EscapeDataString(postLogoutRedirectUri)}"
                };

                context.Response.Redirect(uri.ToString());
                context.HandleResponse(); // Suppress the default processing
                return Task.CompletedTask;
            };
            if (Convert.ToBoolean(configuration["AuthServer:IsBehindTlsTerminationProxy"])
                || Convert.ToBoolean(configuration["AuthServer:SpecifyOidcParameters"]))
            {
                // Rewrite OIDC redirect URI on OpenShift (Staging, Production) environments or if requested
                options.Events.OnRedirectToIdentityProvider = context =>
                {
                    var host = context.Request.Host;
                    var explicitIn = configuration["AuthServer:OidcSignin"] ?? $"https://{host}/signin-oidc";
                    context.ProtocolMessage.SetParameter("redirect_uri", explicitIn);

                    if (!string.IsNullOrEmpty(configuration["AuthServer:IdpHint"]))
                    {
                        context.ProtocolMessage.SetParameter(configuration["AuthServer:IdpHintKey"], configuration["AuthServer:IdpHint"]);
                    }

                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToIdentityProviderForSignOut = ctx =>
                {
                    var host = ctx.Request.Host;
                    var explicitCallback = configuration["AuthServer:OidcSignoutCallback"] ?? $"https://{host}/signout-callback-oidc";
                    ctx.ProtocolMessage.SetParameter("post_logout_redirect_uri", explicitCallback);

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


            options.StyleBundles.Configure(
                NotificationsBundles.Styles.Notifications,
                bundle =>
                {
                    bundle.AddContributors(typeof(NotificationsStyleBundleContributor));
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

    private static bool IsUserAuthenticated(HttpRequest request)
    {
        var currentUser = request.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
        return currentUser?.IsAuthenticated ?? false;
    }

    private static bool IsProfilingAllowed(IWebHostEnvironment env, IConfiguration configuration) =>
        !configuration.GetValue("MiniProfiler:Disabled", false) && (env.IsDevelopment() || env.IsEnvironment("Test"));

    private static bool ShouldEnableDebugFeatures(IWebHostEnvironment env)
    {
        // Debug features (Developer Exception Page, PII logging) are only enabled in non-production environments
        // Production environment will use custom error pages without PII exposure
        return !env.IsProduction();
    }

    private static void ConfigureMiniProfiler(ServiceConfigurationContext context, IConfiguration configuration)
    {
        if (!IsProfilingAllowed(context.Services.GetHostingEnvironment(), configuration))
        {
            return;
        }

        context.Services.Configure<AbpLayoutHookOptions>(options =>
        {
            options.Add(LayoutHooks.Body.Last, typeof(MiniProfilerViewComponent));
        });

        context.Services.AddMiniProfiler(options =>
        {
            options.RouteBasePath = configuration.GetValue("MiniProfiler:RouteBasePath", "/profiler");
            options.EnableMvcViewProfiling = configuration.GetValue("MiniProfiler:ViewProfiling", true);
            options.EnableMvcFilterProfiling = configuration.GetValue("MiniProfiler:FilterProfiling", true);
            options.TrackConnectionOpenClose = configuration.GetValue("MiniProfiler:TrackConnectionOpenClose", true);
            // Optional MiniProfiler Debug Mode - Heavy Memory Use - Not Recommended
            options.EnableDebugMode = configuration.GetValue("MiniProfiler:DebugMode", false);

            ((MemoryCacheStorage)options.Storage).CacheDuration
                = TimeSpan.FromMinutes(configuration.GetValue("MiniProfiler:CacheDuration", 30));

            options.PopupRenderPosition = StackExchange.Profiling.RenderPosition.Right;
            options.PopupStartHidden = true;
            options.PopupToggleKeyboardShortcut = configuration.GetValue("MiniProfiler:PopupToggleKeyboardShortcut", "Alt+P") ?? "Alt+P";
            options.PopupShowTimeWithChildren = true;
            options.ShowControls = true;
            options.PopupShowTrivial = true;

            options.SqlFormatter = new StackExchange.Profiling.SqlFormatters.InlineFormatter();
            options.ColorScheme = ColorScheme.Dark;

            options.IgnoredPaths.AddIfNotContains("/libs/");
            options.IgnoredPaths.AddIfNotContains("/themes/");
            options.IgnoredPaths.AddIfNotContains("/profiler/");
            options.IgnoredPaths.AddIfNotContains("/Abp/");
            options.IgnoredPaths.AddIfNotContains("/Index");

            options.ShouldProfile = (request) =>
                !request.Path.Equals("/")
                && !request.Path.StartsWithSegments("/profiler")
                && !request.Path.StartsWithSegments("/healthz")
                && !request.Path.StartsWithSegments("/api/chefs");

            options.UserIdProvider = static (request) =>
            {

                var currentUser = request.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
                return currentUser?.FindClaimValue("idir_username") ?? "NO_USERNAME";
            };

            options.ResultsAuthorize = IsUserAuthenticated;
            options.ResultsListAuthorize = IsUserAuthenticated;

        }).AddEntityFramework();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();
        var configuration = context.GetConfiguration();

        if (ShouldEnableDebugFeatures(env))
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
        if (IsProfilingAllowed(env, configuration))
        {
            app.UseMiniProfiler();
        }
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

        // If both Redis and Data Protection are enabled then we can enable this session middleware
        if (Convert.ToBoolean(configuration["Redis:IsEnabled"])
            && Convert.ToBoolean(configuration["DataProtection:IsEnabled"]))
        {
            app.UseSession();
        }
    }
}

using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.Extensions;
using Elsa.Webhooks.Extensions;
using System.Net.Http.Headers;
using Unity.Portal.Web.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddElsa(elsa =>
{
    // Configure management feature to use EF Core.
    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef => ef.UseSqlite()));

    // Expose API endpoints.
    elsa.UseWorkflowsApi();

    // Add services for HTTP activities and workflow middleware.
    elsa.UseHttp();

    // Configure identity so that we can create a default admin user.
    elsa.UseIdentity(identity =>
    {
        var configuration = builder.Configuration;
        var identitySection = configuration.GetSection("Identity");
        var identityTokenSection = identitySection.GetSection("Tokens");
        
        identity.IdentityOptions = options => identitySection.Bind(options);
        identity.TokenOptions = options => identityTokenSection.Bind(options);
        identity.UseConfigurationBasedUserProvider(options => identitySection.Bind(options));
        identity.UseConfigurationBasedApplicationProvider(options => identitySection.Bind(options));
        identity.UseConfigurationBasedRoleProvider(options => identitySection.Bind(options));
    });

    // Use default authentication (JWT + API Key).
    elsa.UseDefaultAuthentication();
    // If we wanted to use the key of Authorization: ApiKey 00000000-0000-0000-0000-000000000000
    //elsa.UseDefaultAuthentication(auth => auth.UseAdminApiKey());

    elsa.UseWebhooks(webhooks => webhooks.WebhookOptions = options => builder.Configuration.GetSection("Webhooks").Bind(options));
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Razor pages.
builder.Services.AddRazorPages();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "_myAllowSpecificOrigins",
                      policy =>
                      {
                          policy.WithOrigins("*");
                      });
});

builder.Services.AddHttpClient<ApiController>(httpClient =>
{
    var configuration = builder.Configuration;
    var url = configuration["Elsa:ServerUrl"]!.TrimEnd('/') + '/';
    var apiKey = configuration["Elsa:ApiKey"]!;
    httpClient.BaseAddress = new Uri(url);
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", apiKey);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseCors(builder =>
{
    builder.WithOrigins("http://localhost:8081", "https://localhost:7131")
    .AllowAnyMethod()
    .AllowAnyHeader();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseWorkflowsApi();
app.UseWorkflows();
app.MapRazorPages();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

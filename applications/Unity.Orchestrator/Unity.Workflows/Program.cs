using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.Extensions;
using Elsa.Webhooks.Extensions;
using Elsa.EntityFrameworkCore.Modules.Runtime;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddElsa(elsa =>
{
    var configuration = builder.Configuration;
    var connectionString = configuration.GetValue<String>("DBConnectionString") ?? "";
    // Configure management feature to use EF Core.
    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef => ef.UsePostgreSql(connectionString)));

    // Expose API endpoints.
    elsa.UseWorkflowsApi();

    // Add services for HTTP activities and workflow middleware.
    elsa.UseHttp();

    // Configure identity so that we can create a default admin user.
    elsa.UseIdentity(identity =>
    {
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

    elsa.UseWorkflowRuntime(runtime =>
     {
         runtime.UseDefaultRuntime(dr => dr.UseEntityFrameworkCore(ef => ef.UsePostgreSql(connectionString)));
         runtime.UseExecutionLogRecords(e => e.UseEntityFrameworkCore(ef => ef.UsePostgreSql(connectionString)));
         runtime.UseAsyncWorkflowStateExporter();
     });


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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseWorkflowsApi();
app.UseWorkflows();
app.MapRazorPages();

app.UseCors(builder =>
{
    builder.WithOrigins("http://localhost:8081", "https://localhost:7131")
    .AllowAnyMethod()
    .AllowAnyHeader();
});

app.UseAuthentication();
app.UseAuthorization();

//app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
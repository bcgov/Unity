using Microsoft.EntityFrameworkCore;
using Unity.GrantManager.Applications;
using Unity.GrantManager.ApplicationUserRoles;
using Unity.GrantManager.GrantPrograms;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;

namespace Unity.GrantManager.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public class GrantManagerDbContext :
    AbpDbContext<GrantManagerDbContext>,
    IIdentityDbContext,
    ITenantManagementDbContext
{
    /* Add DbSet properties for your Aggregate Roots / Entities here. */

    #region Entities from the modules

    /* Notice: We only implemented IIdentityDbContext and ITenantManagementDbContext
     * and replaced them for this DbContext. This allows you to perform JOIN
     * queries for the entities of these modules over the repositories easily. You
     * typically don't need that for other modules. But, if you need, you can
     * implement the DbContext interface of the needed module and use ReplaceDbContext
     * attribute just like IIdentityDbContext and ITenantManagementDbContext.
     *
     * More info: Replacing a DbContext of a module ensures that the related module
     * uses this DbContext on runtime. Otherwise, it will use its own DbContext class.
     */

    //Identity
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }

    // Tenant Management
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }

    #endregion

    #region Domain Entities
    public DbSet<GrantProgram> GrantPrograms { get; set; }

    public DbSet<GrantApplication> GrantApplications { get; set; }

    public DbSet<Intake> Intakes { get; set; }
    public DbSet<ApplicationForm> ApplicationForms { get; set; }

    #endregion

    public GrantManagerDbContext(DbContextOptions<GrantManagerDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureFeatureManagement();
        builder.ConfigureTenantManagement();

        /* Configure your own tables/entities inside here */

        //builder.Entity<YourEntity>(b =>
        //{
        //    b.ToTable(GrantManagerConsts.DbTablePrefix + "YourEntities", GrantManagerConsts.DbSchema);
        //    b.ConfigureByConvention(); //auto configure for the base class props
        //    //...
        //});

              

        builder.Entity<GrantProgram>(b =>
        {
            b.ToTable(GrantManagerConsts.DbTablePrefix + "GrantProgram",
                GrantManagerConsts.DbSchema);
           
            b.ConfigureByConvention();
            
            b.Property(x => x.ProgramName)
                .IsRequired()
                .HasMaxLength(250);

            b.HasIndex(x => x.ProgramName);
        });

        builder.Entity<Applicant>(b =>
        {
            b.ToTable(GrantManagerConsts.DbTablePrefix + "Applicant",
                GrantManagerConsts.DbSchema);
           ;
            b.ConfigureByConvention();
            
            b.Property(x => x.ApplicantName)
                .IsRequired()
                .HasMaxLength(250);

            b.HasIndex(x => x.ApplicantName);
        });

        builder.Entity<Intake>(b =>
        {
            b.ToTable(GrantManagerConsts.DbTablePrefix + "Intake",
                GrantManagerConsts.DbSchema);
            
            b.ConfigureByConvention(); //auto configure for the base class props
            b.Property(x => x.IntakeName).IsRequired().HasMaxLength(250);
        });

        builder.Entity<ApplicationForm>(b =>
        {
            b.ToTable(GrantManagerConsts.DbTablePrefix + "ApplicationForm",
                GrantManagerConsts.DbSchema);
            
            b.ConfigureByConvention(); //auto configure for the base class props
            b.Property(x => x.ApplicationFormName).IsRequired().HasMaxLength(250);

            b.HasOne<Intake>().WithMany().HasForeignKey(x => x.IntakeId).IsRequired();
        });

        builder.Entity<Application>(b =>
        {
            b.ToTable(GrantManagerConsts.DbTablePrefix + "Application",
                GrantManagerConsts.DbSchema);
            
            b.ConfigureByConvention(); //auto configure for the base class props
            b.Property(x => x.ApplicationName).IsRequired().HasMaxLength(250);
            b.Property(x => x.Payload).HasColumnType("jsonb");
            b.HasOne<ApplicationForm>().WithMany().HasForeignKey(x => x.ApplicationFormId).IsRequired();
            b.HasOne<Applicant>().WithMany().HasForeignKey(x => x.ApplicantId).IsRequired();
        });

         builder.Entity<ApplicantAgent>(b =>
            {
                b.ToTable(GrantManagerConsts.DbTablePrefix + "ApplicantAgent",
                    GrantManagerConsts.DbSchema);
                
                b.ConfigureByConvention(); //auto configure for the base class props                             
                b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).IsRequired();
                b.HasOne<Applicant>().WithMany().HasForeignKey(x => x.ApplicantId).IsRequired();
            });

        builder.Entity<FormSubmission>(b =>
        {
            b.ToTable(GrantManagerConsts.DbTablePrefix + "FormSubmission",
                GrantManagerConsts.DbSchema);
            
            b.ConfigureByConvention(); //auto configure for the base class props                             
            b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).IsRequired();
            b.HasOne<Application>().WithMany().HasForeignKey(x => x.ApplicationId).IsRequired();
        });

        builder.Entity<AdjudicationAssessment>(b =>
        {
            b.ToTable(GrantManagerConsts.DbTablePrefix + "AdjudicationAssessment",
                GrantManagerConsts.DbSchema);
            
            b.ConfigureByConvention(); //auto configure for the base class props                             
            b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).IsRequired();
            b.HasOne<Application>().WithMany().HasForeignKey(x => x.ApplicationId).IsRequired();
        });

        builder.Entity<ApplicationAssignment>(b =>
        {
            b.ToTable(GrantManagerConsts.DbTablePrefix + "ApplicationAssignment",
                GrantManagerConsts.DbSchema);
            
            b.ConfigureByConvention(); //auto configure for the base class props                             
            b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).IsRequired();
            b.HasOne<Application>().WithMany().HasForeignKey(x => x.ApplicationId).IsRequired();
        });

        var allEntityTypes = builder.Model.GetEntityTypes();
        foreach (var t in allEntityTypes)
        {
            if (t.ClrType != typeof(ExtraPropertyDictionary))
            {
                var entityBuilder = builder.Entity(t.ClrType);
                
                entityBuilder.TryConfigureExtraProperties();
            }
        }

    }
}

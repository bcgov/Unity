using Microsoft.EntityFrameworkCore;
using System.Linq;
using Unity.GrantManager.Locality;
using Unity.GrantManager.Tokens;
using Unity.GrantManager.Intakes;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
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

    public DbSet<Sector> Sectors { get; set; }
    public DbSet<SubSector> SubSectors { get; set; }
    public DbSet<EconomicRegion> EconomicRegion { get; set; }
    public DbSet<ElectoralDistrict> ElectoralDistricts { get; set; }
    public DbSet<RegionalDistrict> RegionalDistricts { get; set; }
    public DbSet<TenantToken> TenantTokens { get; set; }
    public DbSet<Community> Communities { get; set; }
    public DbSet<ChefsMissedSubmission> ChefsMissedSubmissions { get; set; }


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

    public GrantManagerDbContext(DbContextOptions<GrantManagerDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ConfigurePermissionManagement();
        modelBuilder.ConfigureSettingManagement();
        modelBuilder.ConfigureBackgroundJobs();
        modelBuilder.ConfigureAuditLogging();
        modelBuilder.ConfigureIdentity();
        modelBuilder.ConfigureFeatureManagement();
        modelBuilder.ConfigureTenantManagement();

        /* Configure your own tables/entities inside here */

        modelBuilder.Entity<Sector>(b =>
        {
            b.ToTable(GrantManagerConsts.DbTablePrefix + "Sectors",
                GrantManagerConsts.DbSchema);

            b.ConfigureByConvention();
            b.HasMany(e => e.SubSectors).WithOne(e => e.Sector).HasForeignKey(x => x.SectorId);
        });

        modelBuilder.Entity<SubSector>(b =>
        {
            b.ToTable(GrantManagerConsts.DbTablePrefix + "SubSectors",
                GrantManagerConsts.DbSchema);

            b.ConfigureByConvention();
            b.HasOne(e => e.Sector).WithMany(e => e.SubSectors).HasForeignKey(x => x.SectorId);
        });

        modelBuilder.Entity<EconomicRegion>(b =>
        {
            b.ToTable(GrantManagerConsts.DbTablePrefix + "EconomicRegions",
                GrantManagerConsts.DbSchema);

            b.ConfigureByConvention();
        });

        modelBuilder.Entity<ElectoralDistrict>(b =>
        {
            b.ToTable(GrantManagerConsts.DbTablePrefix + "ElectoralDistricts",
                GrantManagerConsts.DbSchema);

            b.ConfigureByConvention();
        });
        modelBuilder.Entity<Community>(b =>
        {
            b.ToTable(GrantManagerConsts.DbTablePrefix + "Communities",
                GrantManagerConsts.DbSchema);

            b.ConfigureByConvention();
        });
        modelBuilder.Entity<RegionalDistrict>(b =>
        {
            b.ToTable(GrantManagerConsts.DbTablePrefix + "RegionalDistricts",
                GrantManagerConsts.DbSchema);

            b.ConfigureByConvention();
        });
        modelBuilder.Entity<TenantToken>(b =>
        {
            b.ToTable(GrantManagerConsts.DbTablePrefix + "TenantTokens",
                GrantManagerConsts.DbSchema);

            b.ConfigureByConvention();
        });

        modelBuilder.Entity<ChefsMissedSubmission>(b =>
        {
            b.ToTable(GrantManagerConsts.DbTablePrefix + "ChefsMissedSubmissions",
                GrantManagerConsts.DbSchema);

            b.ConfigureByConvention();
        });

        var allEntityTypes = modelBuilder.Model.GetEntityTypes();
        foreach (var type in allEntityTypes.Where(t => t.ClrType != typeof(ExtraPropertyDictionary)).Select(t => t.ClrType))
        {
            var entityBuilder = modelBuilder.Entity(type);
            entityBuilder.TryConfigureExtraProperties();
        }        
        
    }
}

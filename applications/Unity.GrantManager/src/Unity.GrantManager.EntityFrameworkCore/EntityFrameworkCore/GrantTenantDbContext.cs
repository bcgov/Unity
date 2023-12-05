using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.Comments;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Unity.GrantManager.Identity;

namespace Unity.GrantManager.EntityFrameworkCore
{
    [ConnectionStringName("Tenant")]
    public class GrantTenantDbContext : AbpDbContext<GrantTenantDbContext>
    {
        #region Domain Entities
        public DbSet<GrantApplication> GrantApplications { get; set; }
        public DbSet<Intake> Intakes { get; set; }
        public DbSet<ApplicationForm> ApplicationForms { get; set; }
        public DbSet<Applicant> Applicants { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<ApplicationStatus> ApplicationStatuses { get; set; }
        public DbSet<ApplicationUserAssignment> ApplicationUserAssignments { get; set; }
        public DbSet<ApplicationComment> ApplicationComments { get; set; }
        public DbSet<Assessment> Assessments { get; set; }
        public DbSet<AssessmentComment> AssessmentComments { get; set; }
        public DbSet<User> Users { get; set; }

        #endregion

        public GrantTenantDbContext(DbContextOptions<GrantTenantDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "User",
                    GrantManagerConsts.DbSchema);
                b.ConfigureByConvention();
                b.HasIndex(x => x.OidcSub);
            });

            modelBuilder.Entity<Applicant>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "Applicant",
                    GrantManagerConsts.DbSchema);
                b.ConfigureByConvention();
                b.Property(x => x.ApplicantName)
                    .IsRequired()
                    .HasMaxLength(250);

                b.HasIndex(x => x.ApplicantName);
            });

            modelBuilder.Entity<Intake>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "Intake",
                    GrantManagerConsts.DbSchema);

                b.ConfigureByConvention(); //auto configure for the base class props
                b.Property(x => x.IntakeName).IsRequired().HasMaxLength(250);
            });

            modelBuilder.Entity<ApplicationForm>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "ApplicationForm",
                    GrantManagerConsts.DbSchema);

                b.ConfigureByConvention(); //auto configure for the base class props
                b.Property(x => x.ApplicationFormName).IsRequired().HasMaxLength(250);

                b.HasOne<Intake>().WithMany().HasForeignKey(x => x.IntakeId).IsRequired();
            });

            modelBuilder.Entity<ApplicationStatus>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "ApplicationStatus",
                    GrantManagerConsts.DbSchema);

                b.ConfigureByConvention(); //auto configure for the base class props
                                           // TODO: Enum mapping could be better implemented with
                                           // using an int for the ID of ApplicationStatus table
                                           // which maps to the GrantApplicationState enum value
                b.Property(x => x.StatusCode)
                    .IsRequired()
                    .HasMaxLength(250)
                    .HasConversion(new EnumToStringConverter<GrantApplicationState>());
                b.HasIndex(x => x.StatusCode).IsUnique();
            });

            modelBuilder.Entity<Application>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "Application",
                    GrantManagerConsts.DbSchema);

                b.ConfigureByConvention(); //auto configure for the base class props
                b.Property(x => x.ProjectName).IsRequired().HasMaxLength(250);
                b.Property(x => x.Payload).HasColumnType("jsonb");
                b.HasOne<ApplicationForm>().WithMany().HasForeignKey(x => x.ApplicationFormId).IsRequired();
                b.HasOne<Applicant>().WithMany().HasForeignKey(x => x.ApplicantId).IsRequired();

                b.HasOne(a => a.ApplicationStatus)
                    .WithMany(s => s.Applications)
                    .HasForeignKey(x => x.ApplicationStatusId)
                    .IsRequired();
            });

            modelBuilder.Entity<Address>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "Address", GrantManagerConsts.DbSchema);
                b.ConfigureByConvention(); //auto configure for the base class props
                b.HasOne<Applicant>().WithMany().HasForeignKey(x => x.ApplicantId);
            });

            modelBuilder.Entity<ApplicantAgent>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "ApplicantAgent",
                    GrantManagerConsts.DbSchema);

                b.ConfigureByConvention(); //auto configure for the base class props                             
                b.HasOne<User>().WithMany().HasPrincipalKey(x => x.OidcSub).HasForeignKey(x => x.OidcSubUser).IsRequired();
                b.HasOne<Applicant>().WithMany().HasForeignKey(x => x.ApplicantId).IsRequired();
            });

            modelBuilder.Entity<ApplicationFormSubmission>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "ApplicationFormSubmission",
                    GrantManagerConsts.DbSchema);

                b.ConfigureByConvention(); //auto configure for the base class props                             
                b.HasOne<Applicant>().WithMany().HasForeignKey(x => x.ApplicantId).IsRequired();
                b.HasOne<ApplicationForm>().WithMany().HasForeignKey(x => x.ApplicationFormId).IsRequired();
            });

            modelBuilder.Entity<ApplicationComment>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "ApplicationComment", GrantManagerConsts.DbSchema);

                b.ConfigureByConvention();
                b.HasOne<Application>().WithMany().HasForeignKey(x => x.ApplicationId).IsRequired();

                b.HasOne<User>()
                   .WithMany()
                   .HasPrincipalKey(x => x.Id)
                   .HasForeignKey(x => x.CommenterId);
            });

            modelBuilder.Entity<ApplicationAttachment>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "ApplicationAttachment", GrantManagerConsts.DbSchema);

                b.ConfigureByConvention();
                b.HasOne<Application>().WithMany().HasForeignKey(x => x.ApplicationId).IsRequired();
            });

            modelBuilder.Entity<Assessment>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "Assessment", GrantManagerConsts.DbSchema);
                b.ConfigureByConvention();

                b.HasOne<Application>()
                    .WithMany()
                    .HasForeignKey(x => x.ApplicationId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.NoAction);

                b.HasOne<User>()
                    .WithMany()
                    .HasPrincipalKey(x => x.Id)
                    .HasForeignKey(x => x.AssessorId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.NoAction);

                b.Property(x => x.Status)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasConversion(new EnumToStringConverter<AssessmentState>());
            });

            modelBuilder.Entity<AssessmentAttachment>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "AssessmentAttachment", GrantManagerConsts.DbSchema);

                b.ConfigureByConvention();
                b.HasOne<Assessment>().WithMany().HasForeignKey(x => x.AssessmentId).IsRequired();
            });

            modelBuilder.Entity<AssessmentComment>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "AssessmentComment", GrantManagerConsts.DbSchema);
                b.HasOne<Assessment>().WithMany().HasForeignKey(x => x.AssessmentId).IsRequired();

                b.HasOne<User>()
                    .WithMany()
                    .HasPrincipalKey(x => x.Id)
                    .HasForeignKey(x => x.CommenterId);
            });

            modelBuilder.Entity<ApplicationUserAssignment>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "ApplicationUserAssignment",
                    GrantManagerConsts.DbSchema);

                b.ConfigureByConvention(); //auto configure for the base class props
                b.HasOne<Application>().WithMany().HasForeignKey(x => x.ApplicationId).IsRequired();

                b.HasOne<User>()
                    .WithMany()
                    .HasPrincipalKey(x => x.Id)
                    .HasForeignKey(x => x.AssigneeId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.NoAction);
            });

            var allEntityTypes = modelBuilder.Model.GetEntityTypes();
            foreach (var type in allEntityTypes.Where(t => t.ClrType != typeof(ExtraPropertyDictionary)).Select(t => t.ClrType))
            {
                var entityBuilder = modelBuilder.Entity(type);
                entityBuilder.TryConfigureExtraProperties();
            }
        }
    }
}

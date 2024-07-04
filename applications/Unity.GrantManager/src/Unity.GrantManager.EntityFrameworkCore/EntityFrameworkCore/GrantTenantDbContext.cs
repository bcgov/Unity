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
using Unity.Payments.EntityFrameworkCore;
using Unity.Flex.EntityFrameworkCore;
using Unity.Notifications.EntityFrameworkCore;

namespace Unity.GrantManager.EntityFrameworkCore
{
    [ConnectionStringName(GrantManagerConsts.DefaultTenantConnectionStringName)]
    public class GrantTenantDbContext : AbpDbContext<GrantTenantDbContext>
    {
        #region Domain Entities
        public DbSet<Intake> Intakes { get; set; }
        public DbSet<ApplicationForm> ApplicationForms { get; set; }
        public DbSet<ApplicationFormVersion> ApplicationFormVersions { get; set; }
        public DbSet<Applicant> Applicants { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<ApplicationStatus> ApplicationStatuses { get; set; }
        public DbSet<ApplicationAssignment> ApplicationUserAssignments { get; set; }
        public DbSet<ApplicationChefsFileAttachment> ApplicationChefsFileAttachments { get; set; }
        public DbSet<ApplicationComment> ApplicationComments { get; set; }
        public DbSet<Assessment> Assessments { get; set; }
        public DbSet<AssessmentComment> AssessmentComments { get; set; }
        public DbSet<Person> Persons { get; set; }
        public DbSet<ApplicantAddress> ApplicantAddresses { get; set; }
        public DbSet<ApplicationTags> ApplicationTags { get; set; }
        public DbSet<ApplicantAgent> ApplicantAgents { get; set; }
        public DbSet<ApplicationAttachment> ApplicationAttachments { get; set; }
        public DbSet<ApplicationFormSubmission> ApplicationFormSubmissions { get; set; }
        public DbSet<AssessmentAttachment> AssessmentAttachments { get; set; }
        public DbSet<ApplicationContact> ApplicationContacts { get; set; }
        public DbSet<ApplicationLink> ApplicationLinks { get; set; }
        #endregion

        public GrantTenantDbContext(DbContextOptions<GrantTenantDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Person>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "Persons",
                    GrantManagerConsts.DbSchema);
                b.ConfigureByConvention();
                b.HasIndex(x => x.OidcSub);
            });

            modelBuilder.Entity<Applicant>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "Applicants",
                    GrantManagerConsts.DbSchema);
                b.ConfigureByConvention();
                b.Property(x => x.ApplicantName)
                    .IsRequired()
                    .HasMaxLength(600);

                b.HasIndex(x => x.ApplicantName);

                b.HasMany<ApplicantAddress>()
                    .WithOne(s => s.Applicant)
                    .HasForeignKey(x => x.ApplicantId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Intake>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "Intakes",
                    GrantManagerConsts.DbSchema);

                b.ConfigureByConvention(); //auto configure for the base class props
                b.Property(x => x.IntakeName).IsRequired().HasMaxLength(255);
            });

            modelBuilder.Entity<ApplicationForm>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "ApplicationForms",
                    GrantManagerConsts.DbSchema);

                b.ConfigureByConvention(); //auto configure for the base class props
                b.Property(x => x.ApplicationFormName).IsRequired().HasMaxLength(255);

                b.HasOne<Intake>().WithMany().HasForeignKey(x => x.IntakeId).IsRequired();
            });

            modelBuilder.Entity<ApplicationFormVersion>(b =>
            {
                b.ToTable(GrantManagerConsts.DbTablePrefix + "ApplicationFormVersion",
                    GrantManagerConsts.DbSchema);

                b.ConfigureByConvention(); //auto configure for the base class props
                b.HasOne<ApplicationForm>().WithMany().HasForeignKey(x => x.ApplicationFormId).IsRequired();
            });

            modelBuilder.Entity<ApplicationStatus>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "ApplicationStatuses",
                    GrantManagerConsts.DbSchema);

                b.ConfigureByConvention();
                b.Property(x => x.StatusCode)
                    .IsRequired()
                    .HasMaxLength(250)
                    .HasConversion(new EnumToStringConverter<GrantApplicationState>());
                b.HasIndex(x => x.StatusCode).IsUnique();
            });

            modelBuilder.Entity<Application>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "Applications",
                    GrantManagerConsts.DbSchema);

                b.ConfigureByConvention(); //auto configure for the base class props
                b.Property(x => x.ProjectName).IsRequired().HasMaxLength(255);
                b.Property(x => x.Payload).HasColumnType("jsonb");
                b.HasOne(a => a.ApplicationForm).WithMany().HasForeignKey(x => x.ApplicationFormId).IsRequired().OnDelete(DeleteBehavior.NoAction);
                b.HasOne(a => a.Applicant).WithMany().HasForeignKey(x => x.ApplicantId).IsRequired().OnDelete(DeleteBehavior.NoAction);
                b.HasOne(a => a.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.NoAction);
                b.HasMany(a => a.ApplicationAssignments).WithOne(s => s.Application).HasForeignKey(x => x.ApplicationId).IsRequired().OnDelete(DeleteBehavior.NoAction);
                b.HasMany(a => a.ApplicationTags).WithOne(s => s.Application).HasForeignKey(x => x.ApplicationId).IsRequired().OnDelete(DeleteBehavior.NoAction);
                b.HasMany(a => a.Assessments).WithOne(s => s.Application).HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(a => a.ApplicantAgent).WithOne(s => s.Application);

                b.HasOne(a => a.ApplicationStatus)
                    .WithMany(s => s.Applications)
                    .HasForeignKey(x => x.ApplicationStatusId)
                    .IsRequired();
            });

            modelBuilder.Entity<ApplicantAddress>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "ApplicantAddresses", GrantManagerConsts.DbSchema);
                b.ConfigureByConvention(); //auto configure for the base class props                

                b.HasOne(x => x.Applicant)
                    .WithMany(s => s.ApplicantAddresses)
                    .HasForeignKey(s => s.ApplicantId)
                    .IsRequired();
            });

            modelBuilder.Entity<ApplicantAgent>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "ApplicantAgents",
                    GrantManagerConsts.DbSchema);

                b.ConfigureByConvention(); //auto configure for the base class props                             
                b.HasOne<Person>().WithMany().HasPrincipalKey(x => x.OidcSub).HasForeignKey(x => x.OidcSubUser).IsRequired(false);
                b.HasOne<Applicant>().WithMany().HasForeignKey(x => x.ApplicantId).IsRequired();
            });

            modelBuilder.Entity<ApplicationFormSubmission>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "ApplicationFormSubmissions",
                    GrantManagerConsts.DbSchema);

                b.ConfigureByConvention(); //auto configure for the base class props                             
                b.HasOne<Applicant>().WithMany().HasForeignKey(x => x.ApplicantId).IsRequired();
                b.HasOne<ApplicationForm>().WithMany().HasForeignKey(x => x.ApplicationFormId).IsRequired();
            });

            modelBuilder.Entity<ApplicationComment>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "ApplicationComments", GrantManagerConsts.DbSchema);

                b.ConfigureByConvention();
                b.HasOne<Application>().WithMany().HasForeignKey(x => x.ApplicationId).IsRequired();

                b.HasOne<Person>()
                   .WithMany()
                   .HasPrincipalKey(x => x.Id)
                   .HasForeignKey(x => x.CommenterId);
            });

            modelBuilder.Entity<ApplicationAttachment>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "ApplicationAttachments", GrantManagerConsts.DbSchema);

                b.ConfigureByConvention();
                b.HasOne<Application>().WithMany().HasForeignKey(x => x.ApplicationId).IsRequired();
            });

            modelBuilder.Entity<ApplicationChefsFileAttachment>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "ApplicationChefsFileAttachments", GrantManagerConsts.DbSchema);

                b.ConfigureByConvention();
                b.HasOne<Application>().WithMany().HasForeignKey(x => x.ApplicationId).IsRequired();
            });

            modelBuilder.Entity<Assessment>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "Assessments", GrantManagerConsts.DbSchema);
                b.ConfigureByConvention();

                b.HasOne<Person>()
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
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "AssessmentAttachments", GrantManagerConsts.DbSchema);

                b.ConfigureByConvention();
                b.HasOne<Assessment>().WithMany().HasForeignKey(x => x.AssessmentId).IsRequired();
            });

            modelBuilder.Entity<AssessmentComment>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "AssessmentComments", GrantManagerConsts.DbSchema);
                b.HasOne<Assessment>().WithMany().HasForeignKey(x => x.AssessmentId).IsRequired();

                b.HasOne<Person>()
                    .WithMany()
                    .HasPrincipalKey(x => x.Id)
                    .HasForeignKey(x => x.CommenterId);
            });

            modelBuilder.Entity<ApplicationAssignment>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "ApplicationAssignments",
                    GrantManagerConsts.DbSchema);

                b.ConfigureByConvention();
            });

            modelBuilder.Entity<ApplicationTags>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "ApplicationTags",
                    GrantManagerConsts.DbSchema);

                b.ConfigureByConvention();
                b.Property(x => x.Text)
                    .IsRequired()
                    .HasMaxLength(250);
            });

            modelBuilder.Entity<ApplicationContact>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "ApplicationContact",
                    GrantManagerConsts.DbSchema);

                b.ConfigureByConvention();
                b.HasOne<Application>().WithMany().HasForeignKey(x => x.ApplicationId).IsRequired();

            });

            modelBuilder.Entity<ApplicationLink>(b =>
            {
                b.ToTable(GrantManagerConsts.TenantTablePrefix + "ApplicationLinks",
                    GrantManagerConsts.DbSchema);

                b.ConfigureByConvention();
                b.HasOne<Application>().WithMany().HasForeignKey(x => x.ApplicationId).IsRequired();

            });

            var allEntityTypes = modelBuilder.Model.GetEntityTypes();
            foreach (var type in allEntityTypes.Where(t => t.ClrType != typeof(ExtraPropertyDictionary)).Select(t => t.ClrType))
            {
                var entityBuilder = modelBuilder.Entity(type);
                entityBuilder.TryConfigureExtraProperties();
            }

            modelBuilder.ConfigurePayments();
            modelBuilder.ConfigureFlex();
            modelBuilder.ConfigureNotifications();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Unity.GrantManager.Applications;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace Unity.GrantManager.EntityFrameworkCore;

public static class ApplicantMergeModelBuilderExtensions
{
    public static void ConfigureApplicantMerges(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicantMergeOperation>(builder =>
        {
            builder.ToTable(
                GrantManagerConsts.TenantTablePrefix + "ApplicantMergeOperations",
                GrantManagerConsts.TenantDbSchema);
            builder.ConfigureByConvention();

            builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            builder.Property(item => item.Source).HasConversion<string>().HasMaxLength(32).IsRequired();
            builder.Property(item => item.PrincipalStateBefore).HasColumnType("jsonb").IsRequired();
            builder.Property(item => item.PrincipalStateAfter).HasColumnType("jsonb").IsRequired();
            builder.Property(item => item.SecondaryStateBefore).HasColumnType("jsonb").IsRequired();
            builder.Property(item => item.SecondaryStateAfter).HasColumnType("jsonb").IsRequired();
            builder.Property(item => item.ReversalReason).HasMaxLength(1000);

            builder.HasOne<Applicant>()
                .WithMany()
                .HasForeignKey(item => item.PrincipalApplicantId)
                .OnDelete(DeleteBehavior.NoAction);
            builder.HasOne<Applicant>()
                .WithMany()
                .HasForeignKey(item => item.SecondaryApplicantId)
                .OnDelete(DeleteBehavior.NoAction);
            builder.HasMany(item => item.ApplicationChanges)
                .WithOne()
                .HasForeignKey(item => item.ApplicantMergeOperationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(item => new { item.TenantId, item.PrincipalApplicantId, item.Status });
            builder.HasIndex(item => new { item.TenantId, item.SecondaryApplicantId, item.Status });
            builder.HasIndex(item => new { item.TenantId, item.MergedAt });
        });

        modelBuilder.Entity<ApplicantMergeApplicationChange>(builder =>
        {
            builder.ToTable(
                GrantManagerConsts.TenantTablePrefix + "ApplicantMergeApplicationChanges",
                GrantManagerConsts.TenantDbSchema);
            builder.ConfigureByConvention();

            builder.Property(item => item.RelatedRecordsSnapshot).HasColumnType("jsonb").IsRequired();
            builder.HasOne<Application>()
                .WithMany()
                .HasForeignKey(item => item.ApplicationId)
                .OnDelete(DeleteBehavior.NoAction);
            builder.HasIndex(item => new { item.ApplicantMergeOperationId, item.ApplicationId }).IsUnique();
            builder.HasIndex(item => new { item.TenantId, item.ApplicationId });
        });
    }
}

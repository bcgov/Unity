using Microsoft.EntityFrameworkCore;
using Unity.Flex.Domain;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.WorksheetLinks;
using Unity.Flex.Domain.Worksheets;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace Unity.Flex.EntityFrameworkCore;

public static class FlexDbContextModelCreatingExtensions
{
    public static void ConfigureFlex(
        this ModelBuilder modelBuilder)
    {
        Check.NotNull(modelBuilder, nameof(modelBuilder));
        ConfigureScoresheets(modelBuilder);
        ConfigureWorksheets(modelBuilder);
    }

    private static void ConfigureScoresheets(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Scoresheet>(b =>
        {
            b.ToTable(FlexDbProperties.DbTablePrefix + "Scoresheets",
                FlexDbProperties.DbSchema);

            b.ConfigureByConvention();

            b.HasMany(e => e.Sections)
                .WithOne(e => e.Scoresheet)
                .HasForeignKey(x => x.ScoresheetId);

            b.HasMany(e => e.Instances)
                .WithOne(e => e.Scoresheet)
                .HasForeignKey(x => x.ScoresheetId);
        });

        modelBuilder.Entity<Question>(b =>
        {
            b.ToTable(FlexDbProperties.DbTablePrefix + "Questions",
                FlexDbProperties.DbSchema);

            b.ConfigureByConvention();

            b.HasMany(e => e.Answers)
               .WithOne(e => e.Question)
               .HasForeignKey(x => x.QuestionId);
        });

        modelBuilder.Entity<Answer>(b =>
        {
            b.ToTable(FlexDbProperties.DbTablePrefix + "Answers",
                FlexDbProperties.DbSchema);

            b.ConfigureByConvention();
        });

        modelBuilder.Entity<ScoresheetSection>(b =>
        {
            b.ToTable(FlexDbProperties.DbTablePrefix + "ScoresheetSections",
                FlexDbProperties.DbSchema);

            b.ConfigureByConvention();

            b.HasMany(e => e.Fields)
                .WithOne(e => e.Section)
                .HasForeignKey(x => x.SectionId);
        });

        modelBuilder.Entity<ScoresheetInstance>(b =>
        {
            b.ToTable(FlexDbProperties.DbTablePrefix + "ScoresheetInstances",
                FlexDbProperties.DbSchema);

            b.ConfigureByConvention();

            b.HasMany(e => e.Answers)
                .WithOne()
                .HasForeignKey(x => x.ScoresheetInstanceId);
        });
    }

    private static void ConfigureWorksheets(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Worksheet>(b =>
        {
            b.ToTable(FlexDbProperties.DbTablePrefix + "Worksheets",
                FlexDbProperties.DbSchema);

            b.ConfigureByConvention();

            b.HasMany(e => e.Sections)
                .WithOne(e => e.Worksheet)
                .HasForeignKey(x => x.WorksheetId);

            b.HasMany(e => e.Links)
                .WithOne()
                .HasForeignKey(x => x.WorksheetId);
        });

        modelBuilder.Entity<WorksheetLink>(b =>
        {
            b.ToTable(FlexDbProperties.DbTablePrefix + "WorksheetLinks",
                FlexDbProperties.DbSchema);

            b.HasOne(e => e.Worksheet)
                .WithMany(e => e.Links)
                .HasForeignKey(e => e.WorksheetId);

            b.ConfigureByConvention();
        });

        modelBuilder.Entity<CustomField>(b =>
        {
            b.ToTable(FlexDbProperties.DbTablePrefix + "CustomFields",
                FlexDbProperties.DbSchema);

            b.ConfigureByConvention();
        });

        modelBuilder.Entity<CustomFieldValue>(b =>
        {
            b.ToTable(FlexDbProperties.DbTablePrefix + "CustomFieldValues",
                FlexDbProperties.DbSchema);

            b.ConfigureByConvention();
        });

        modelBuilder.Entity<WorksheetSection>(b =>
        {
            b.ToTable(FlexDbProperties.DbTablePrefix + "WorksheetSections",
                FlexDbProperties.DbSchema);

            b.ConfigureByConvention();

            b.HasMany(e => e.Fields)
                .WithOne(e => e.Section)
                .HasForeignKey(x => x.SectionId);
        });

        modelBuilder.Entity<WorksheetInstance>(b =>
        {
            b.ToTable(FlexDbProperties.DbTablePrefix + "WorksheetInstances",
                FlexDbProperties.DbSchema);

            b.ConfigureByConvention();

            b.HasMany(e => e.Values)
                .WithOne()
                .HasForeignKey(x => x.WorksheetInstanceId);
        });

        modelBuilder.Entity<CustomFieldValue>(b =>
        {
            b.ToTable(FlexDbProperties.DbTablePrefix + "CustomFieldValues",
                FlexDbProperties.DbSchema);

            b.ConfigureByConvention();
        });
    }
}

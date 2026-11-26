using Microsoft.EntityFrameworkCore;
using Unity.AI.Domain;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace Unity.AI.EntityFrameworkCore;

public static class AIDbContextModelCreatingExtensions
{
    public static void ConfigureAI(this ModelBuilder modelBuilder)
    {
        Check.NotNull(modelBuilder, nameof(modelBuilder));

        modelBuilder.Entity<AIPrompt>(b =>
        {
            b.ToTable(AIDbProperties.DbTablePrefix + "AIPrompts", AIDbProperties.DbSchema);

            b.ConfigureByConvention();

            b.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            b.Property(x => x.VersionNumber)
                .IsRequired();

            b.Property(x => x.SystemPrompt)
                .IsRequired()
                .HasColumnType("text");

            b.Property(x => x.UserPrompt)
                .IsRequired()
                .HasColumnType("text");

            b.Property(x => x.MetadataJson)
                .IsRequired()
                .HasColumnType("jsonb")
                .HasDefaultValue("{}");

            b.Property(x => x.IsActive)
                .IsRequired();

            b.HasIndex(x => new { x.TenantId, x.Name, x.VersionNumber })
                .IsUnique();
        });

        modelBuilder.Entity<AIModel>(b =>
        {
            b.ToTable(AIDbProperties.DbTablePrefix + "AIModels", AIDbProperties.DbSchema);

            b.ConfigureByConvention();

            b.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            b.Property(x => x.Provider)
                .IsRequired()
                .HasMaxLength(100);

            b.Property(x => x.IsActive)
                .IsRequired();

            b.Property(x => x.Type)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            b.Property(x => x.ApiVersion)
                .IsRequired()
                .HasMaxLength(32);

            // Names are also unique ignoring case: migration AddAIModelType adds the PostgreSQL index
            // IX_AIModels_Name_Lower on lower("Name") with raw SQL (EF can't model it here).
            b.HasIndex(x => x.Name)
                .IsUnique();
        });

        modelBuilder.Entity<AIOperation>(b =>
        {
            b.ToTable(AIDbProperties.DbTablePrefix + "AIOperations", AIDbProperties.DbSchema);

            b.ConfigureByConvention();

            b.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            b.Property(x => x.ExecutionMode)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            b.Property(x => x.IsActive)
                .IsRequired();

            b.HasIndex(x => x.Name)
                .IsUnique();

            b.HasOne(x => x.AIModel)
                .WithMany()
                .HasForeignKey(x => x.AIModelId)
                .OnDelete(DeleteBehavior.Restrict);

        });
    }
}

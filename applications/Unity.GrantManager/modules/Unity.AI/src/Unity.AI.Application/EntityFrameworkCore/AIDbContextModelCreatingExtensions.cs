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

            b.Property(x => x.IsActive)
                .IsRequired();

            b.Property(x => x.SettingsJson)
                .IsRequired()
                .HasColumnType("jsonb");

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

            b.Property(x => x.CompletionTokens)
                .IsRequired();

            b.Property(x => x.IsActive)
                .IsRequired();

            b.HasIndex(x => x.Name)
                .IsUnique();

            b.HasOne(x => x.AIModel)
                .WithMany()
                .HasForeignKey(x => x.AIModelId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.AIPrompt)
                .WithMany()
                .HasForeignKey(x => x.AIPromptId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

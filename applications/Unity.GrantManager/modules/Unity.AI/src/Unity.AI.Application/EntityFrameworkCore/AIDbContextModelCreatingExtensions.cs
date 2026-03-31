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

            b.Property(x => x.Description)
                .HasMaxLength(2000);

            b.Property(x => x.Type)
                .IsRequired();

            b.HasMany(x => x.Versions)
                .WithOne(x => x.Prompt)
                .HasForeignKey(x => x.PromptId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AIPromptVersion>(b =>
        {
            b.ToTable(AIDbProperties.DbTablePrefix + "AIPromptVersions", AIDbProperties.DbSchema);

            b.ConfigureByConvention();

            b.Property(x => x.SystemPrompt).IsRequired().HasColumnType("text");
            b.Property(x => x.UserPromptTemplate).IsRequired().HasColumnType("text");

            b.Property(x => x.TargetModel)
                .HasMaxLength(100);

            b.Property(x => x.TargetProvider)
                .HasMaxLength(100);

            b.Property(x => x.MetadataJson)
                .HasColumnType("jsonb");

            b.HasIndex(x => new { x.PromptId, x.VersionNumber })
                .IsUnique();
        });
    }
}

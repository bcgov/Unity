using Microsoft.EntityFrameworkCore;
using Volo.Abp;

namespace Unity.Flex.EntityFrameworkCore;

public static class FlexDbContextModelCreatingExtensions
{
    public static void ConfigureFlex(
        this ModelBuilder builder)
#pragma warning disable S125 // Sections of code should not be commented out
    {
        Check.NotNull(builder, nameof(builder));

        /* Configure all entities here. Example:

        builder.Entity<Question>(b =>
        {
            //Configure table & schema name
            b.ToTable(FlexDbProperties.DbTablePrefix + "Questions", FlexDbProperties.DbSchema);

            b.ConfigureByConvention();

            //Properties
            b.Property(q => q.Title).IsRequired().HasMaxLength(QuestionConsts.MaxTitleLength);

            //Relations
            b.HasMany(question => question.Tags).WithOne().HasForeignKey(qt => qt.QuestionId);

            //Indexes
            b.HasIndex(q => q.CreationTime);
        });
        */
    }
#pragma warning restore S125 // Sections of code should not be commented out
}

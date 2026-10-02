using Microsoft.EntityFrameworkCore;
using Shouldly;
using Unity.Notifications.Templates;
using Xunit;

namespace Unity.Notifications.EntityFrameworkCore;

// Verifies the EF Core mapping that the LimitEmailTemplateNameLength migration was generated from
public class EmailTemplateMappingTests
{
    [Fact]
    public void Should_Map_Name_As_Required_With_Max_Length()
    {
        using var context = new NotificationsDbContext(
            new DbContextOptionsBuilder<NotificationsDbContext>().UseSqlite("Data Source=:memory:").Options);

        var nameProperty = context.Model
            .FindEntityType(typeof(EmailTemplate))!
            .FindProperty(nameof(EmailTemplate.Name))!;

        nameProperty.GetMaxLength().ShouldBe(EmailTemplateConsts.MaxNameLength);
        nameProperty.IsNullable.ShouldBeFalse();
    }
}

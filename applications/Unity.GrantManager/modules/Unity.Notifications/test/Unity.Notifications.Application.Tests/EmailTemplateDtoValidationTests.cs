using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Shouldly;
using Xunit;

namespace Unity.Notifications.Templates;

// Covers the [StringLength] rule on EmailTempateDto.Name that ABP enforces on app service input
public class EmailTemplateDtoValidationTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(EmailTemplateConsts.MaxNameLength)]
    public void Should_Pass_Validation_When_Name_Is_Within_Max_Length(int nameLength)
    {
        var dto = new EmailTempateDto { Name = new string('a', nameLength) };

        var results = Validate(dto);

        results.ShouldNotContain(result => result.MemberNames.Contains(nameof(EmailTempateDto.Name)));
    }

    [Fact]
    public void Should_Fail_Validation_When_Name_Exceeds_Max_Length()
    {
        var dto = new EmailTempateDto { Name = new string('a', EmailTemplateConsts.MaxNameLength + 1) };

        var results = Validate(dto);

        results.ShouldContain(result => result.MemberNames.Contains(nameof(EmailTempateDto.Name)));
    }

    // Guards the agreed limit so a change to the constant is deliberate
    [Fact]
    public void Should_Limit_Name_To_Fifty_Characters()
    {
        EmailTemplateConsts.MaxNameLength.ShouldBe(50);
    }

    private static List<ValidationResult> Validate(EmailTempateDto dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        return results;
    }
}

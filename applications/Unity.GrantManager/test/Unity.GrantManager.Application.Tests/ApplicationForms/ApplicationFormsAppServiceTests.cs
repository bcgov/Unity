using Newtonsoft.Json.Linq;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.ApplicationForms
{
    public class ApplicationFormsAppServiceTests : GrantManagerApplicationTestBase
    {
        private readonly IApplicationFormVersionAppService _applicationFormAppService;
        private readonly IApplicationFormVersionRepository _applicationsRepository;

        public ApplicationFormsAppServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _applicationFormAppService = GetRequiredService<IApplicationFormVersionAppService>();
            _applicationsRepository = GetRequiredService<IApplicationFormVersionRepository>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task DeleteWorkSheetMappingByFormName_ShouldRemoveExpected()
        {
            // Arrange
            var formName = "form-v1";

            var formVersion = await _applicationsRepository.InsertAsync(new ApplicationFormVersion()
            {
                ApplicationFormId = GrantManagerTestData.ApplicationForm1_Id, //coming from seed data
                SubmissionHeaderMapping = @"{
                        ""ApplicantName"":""John"",
                        ""form-v1_field1.Text"":""textfield1"",
                        ""form-v1_field2.Text"":""textfield2"",
                        ""ApplicantLastName"":""Smith"",
                    }"
            }, true);

            // Act
            await _applicationFormAppService.DeleteWorkSheetMappingByFormName(formName, formVersion.Id);

            // Assert
            var updatedFormVersion = await _applicationsRepository.GetAsync(formVersion.Id);            
            updatedFormVersion.SubmissionHeaderMapping.ShouldNotBeNullOrEmpty();            
            JObject obj = JObject.Parse(updatedFormVersion.SubmissionHeaderMapping ?? "{}");
            obj.Properties().Count().ShouldBe(2);
            obj.Properties().First().Name.ShouldBe("ApplicantName");
            obj.Properties().Last().Name.ShouldBe("ApplicantLastName");
        }
    }
}

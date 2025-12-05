using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Shouldly;
using System;
using System.IO;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes;
using Xunit;

namespace Unity.GrantManager.Intake
{
    public class BasicIntakeMappingTests : GrantManagerDomainTestBase
    {
        private readonly IIntakeFormSubmissionMapper _intakeFormSubmissionMapper;
        private readonly IApplicationChefsFileAttachmentRepository _applicationChefsFileAttachmentRepository;

        public BasicIntakeMappingTests()
        {
            _applicationChefsFileAttachmentRepository = Substitute.For<IApplicationChefsFileAttachmentRepository>();
            _intakeFormSubmissionMapper = new IntakeFormSubmissionMapper(_applicationChefsFileAttachmentRepository);
        }

        private static dynamic? LoadTestData(string filename)
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Intake", "Mapping", filename);
            var reader = new StreamReader(filePath);
            var jsonStr = reader.ReadToEnd();
            var testData = JsonConvert.DeserializeObject<dynamic>(jsonStr);
            reader.Dispose();
            return testData;
        }


        [Theory]
        [InlineData("basicdatagrid.json", 2)]
        [InlineData("panelinpaneldatagrid.json", 1)]
        public void TestBasicDataGridMapping(string filename, int expectedDatagridCount)
        {
            dynamic? formMapping = LoadTestData(filename);
            string result = _intakeFormSubmissionMapper.InitializeAvailableFormFields(formMapping);
            result.ShouldNotBeNull();

            var parsedJson = JObject.Parse(result);
            int datagridCount = 0;

            foreach (var property in parsedJson.Properties())
            {
                var value = JObject.Parse(property.Value.ToString());
                if (value["type"]?.ToString() == "datagrid")
                {
                    datagridCount++;
                }
            }

            Assert.Equal(expectedDatagridCount, datagridCount);
        }
    }
}

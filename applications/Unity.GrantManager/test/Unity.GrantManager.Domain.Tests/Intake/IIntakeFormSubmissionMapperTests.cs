using Newtonsoft.Json;
using System.IO;
using System;
using Xunit;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Applications;
using NSubstitute;
using System.Collections.Generic;
using Shouldly;

namespace Unity.GrantManager.Intake
{
    public class IIntakeFormSubmissionMapperTests : GrantManagerDomainTestBase
    {
        private readonly IIntakeFormSubmissionMapper _intakeFormSubmissionMapper;
        private readonly IApplicationChefsFileAttachmentRepository _applicationChefsFileAttachmentRepository;

        public IIntakeFormSubmissionMapperTests()
        {
            _applicationChefsFileAttachmentRepository = Substitute.For<IApplicationChefsFileAttachmentRepository>();
            _intakeFormSubmissionMapper = new IntakeFormSubmissionMapper(_applicationChefsFileAttachmentRepository);
        }

        private static dynamic? LoadTestData(string filename)
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Intake\\Files\\" + filename);
            var reader = new StreamReader(filePath);
            var jsonStr = reader.ReadToEnd();
            var testData = JsonConvert.DeserializeObject<dynamic>(jsonStr);
            reader.Dispose();
            return testData;
        }

        [Theory]
        [InlineData("test-submission1.json", 14)]
        [InlineData("test-submission2.json", 20)]
        [InlineData("test-submission3.json", 14)]
        public void ExtractSubmissionFiles_ReturnsExpectedCount(string filename, int expectedCount)
        {
            dynamic? formSubmission = LoadTestData(filename);
            Dictionary<Guid, string> dictionary = _intakeFormSubmissionMapper.ExtractSubmissionFiles(formSubmission);
            dictionary.Count.ShouldBe(expectedCount);
        }
    }
}



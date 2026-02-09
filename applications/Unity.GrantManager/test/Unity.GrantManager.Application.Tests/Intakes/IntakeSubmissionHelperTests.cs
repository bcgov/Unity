using Shouldly;
using System;
using System.Dynamic;
using Xunit;

namespace Unity.GrantManager.Intakes
{
    public class IntakeSubmissionHelperTests
    {
        [Fact]
        public void ExtractOidcSub_WithValidSubmissionDataPath_ReturnsUppercaseSubWithoutIdpSuffix()
        {
            // Arrange - Create a mock dynamic object matching the first path: submission->data->applicantAgent->sub
            dynamic submission = new ExpandoObject();
            submission.submission = new ExpandoObject();
            submission.submission.data = new ExpandoObject();
            submission.submission.data.applicantAgent = new ExpandoObject();
            submission.submission.data.applicantAgent.sub = "abc123xyz@idir";

            // Act
            string result = IntakeSubmissionHelper.ExtractOidcSub(submission);

            // Assert
            result.ShouldBe("ABC123XYZ");
        }

        [Fact]
        public void ExtractOidcSub_WithValidSubmissionDataPath_NoAtSymbol_ReturnsUppercaseSub()
        {
            // Arrange
            dynamic submission = new ExpandoObject();
            submission.submission = new ExpandoObject();
            submission.submission.data = new ExpandoObject();
            submission.submission.data.applicantAgent = new ExpandoObject();
            submission.submission.data.applicantAgent.sub = "abc123xyz";

            // Act
            string result = IntakeSubmissionHelper.ExtractOidcSub(submission);

            // Assert
            result.ShouldBe("ABC123XYZ");
        }

        [Fact]
        public void ExtractOidcSub_FallbackToCreatedBy_ReturnsUppercaseSubWithoutIdpSuffix()
        {
            // Arrange - First path doesn't exist, should fall back to createdBy
            dynamic submission = new ExpandoObject();
            submission.createdBy = "user456@bceid";

            // Act
            string result = IntakeSubmissionHelper.ExtractOidcSub(submission);

            // Assert
            result.ShouldBe("USER456");
        }

        [Fact]
        public void ExtractOidcSub_WithEmptyFirstPath_FallsBackToCreatedBy()
        {
            // Arrange - First path exists but is empty/null
            dynamic submission = new ExpandoObject();
            submission.submission = new ExpandoObject();
            submission.submission.data = new ExpandoObject();
            submission.submission.data.applicantAgent = new ExpandoObject();
            submission.submission.data.applicantAgent.sub = "";
            submission.createdBy = "fallback123@azureadb2c";

            // Act
            string result = IntakeSubmissionHelper.ExtractOidcSub(submission);

            // Assert
            result.ShouldBe("FALLBACK123");
        }

        [Fact]
        public void ExtractOidcSub_WithWhitespaceInFirstPath_FallsBackToCreatedBy()
        {
            // Arrange
            dynamic submission = new ExpandoObject();
            submission.submission = new ExpandoObject();
            submission.submission.data = new ExpandoObject();
            submission.submission.data.applicantAgent = new ExpandoObject();
            submission.submission.data.applicantAgent.sub = "   ";
            submission.createdBy = "backup789@idir";

            // Act
            string result = IntakeSubmissionHelper.ExtractOidcSub(submission);

            // Assert
            result.ShouldBe("BACKUP789");
        }

        [Fact]
        public void ExtractOidcSub_WithNoValidPath_ReturnsEmptyGuidString()
        {
            // Arrange - Neither path exists
            dynamic submission = new ExpandoObject();

            // Act
            string result = IntakeSubmissionHelper.ExtractOidcSub(submission);

            // Assert
            result.ShouldBe(Guid.Empty.ToString());
        }

        [Fact]
        public void ExtractOidcSub_WithNullSubmission_ReturnsEmptyGuidString()
        {
            // Arrange
            dynamic submission = null!;

            // Act
            string result = IntakeSubmissionHelper.ExtractOidcSub(submission);

            // Assert
            result.ShouldBe(Guid.Empty.ToString());
        }

        [Fact]
        public void ExtractOidcSub_WithPartialPath_ReturnsEmptyGuidString()
        {
            // Arrange - Path exists partially but not fully
            dynamic submission = new ExpandoObject();
            submission.submission = new ExpandoObject();
            submission.submission.data = new ExpandoObject();
            // applicantAgent is missing

            // Act
            string result = IntakeSubmissionHelper.ExtractOidcSub(submission);

            // Assert
            result.ShouldBe(Guid.Empty.ToString());
        }

        [Fact]
        public void ExtractOidcSub_WithMultipleAtSymbols_ExtractsBeforeFirstAt()
        {
            // Arrange
            dynamic submission = new ExpandoObject();
            submission.submission = new ExpandoObject();
            submission.submission.data = new ExpandoObject();
            submission.submission.data.applicantAgent = new ExpandoObject();
            submission.submission.data.applicantAgent.sub = "user@email@domain@idir";

            // Act
            string result = IntakeSubmissionHelper.ExtractOidcSub(submission);

            // Assert
            result.ShouldBe("USER");
        }

        [Fact]
        public void ExtractOidcSub_WithMixedCaseSub_ReturnsUppercase()
        {
            // Arrange
            dynamic submission = new ExpandoObject();
            submission.createdBy = "MiXeDcAsE123@bceid";

            // Act
            string result = IntakeSubmissionHelper.ExtractOidcSub(submission);

            // Assert
            result.ShouldBe("MIXEDCASE123");
        }

        [Fact]
        public void ExtractOidcSub_WithNumericSub_ReturnsUppercaseString()
        {
            // Arrange
            dynamic submission = new ExpandoObject();
            submission.submission = new ExpandoObject();
            submission.submission.data = new ExpandoObject();
            submission.submission.data.applicantAgent = new ExpandoObject();
            submission.submission.data.applicantAgent.sub = "123456789@idir";

            // Act
            string result = IntakeSubmissionHelper.ExtractOidcSub(submission);

            // Assert
            result.ShouldBe("123456789");
        }

        [Fact]
        public void ExtractOidcSub_WithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            dynamic submission = new ExpandoObject();
            submission.createdBy = "user-name_123.test@bceid";

            // Act
            string result = IntakeSubmissionHelper.ExtractOidcSub(submission);

            // Assert
            result.ShouldBe("USER-NAME_123.TEST");
        }

        [Fact]
        public void ExtractOidcSub_WithNestedDynamicStructure_SuccessfullyNavigates()
        {
            // Arrange - Build a deeply nested structure
            dynamic submission = new ExpandoObject();
            submission.submission = new ExpandoObject();
            submission.submission.data = new ExpandoObject();
            submission.submission.data.applicantAgent = new ExpandoObject();
            submission.submission.data.applicantAgent.sub = "deep-nested-value@azureadb2c";

            // Act
            string result = IntakeSubmissionHelper.ExtractOidcSub(submission);

            // Assert
            result.ShouldBe("DEEP-NESTED-VALUE");
        }

        [Fact]
        public void ExtractOidcSub_WithAtSymbolAtStart_ReturnsEmptyGuidString()
        {
            // Arrange - @ at the beginning means nothing before it
            dynamic submission = new ExpandoObject();
            submission.createdBy = "@idir";

            // Act
            string result = IntakeSubmissionHelper.ExtractOidcSub(submission);

            // Assert
            result.ShouldBe(Guid.Empty.ToString());
        }

        [Fact]
        public void ExtractOidcSub_WithLongIdentifier_HandlesCorrectly()
        {
            // Arrange
            dynamic submission = new ExpandoObject();
            submission.submission = new ExpandoObject();
            submission.submission.data = new ExpandoObject();
            submission.submission.data.applicantAgent = new ExpandoObject();
            submission.submission.data.applicantAgent.sub = "very-long-user-identifier-with-many-characters-1234567890@idir";

            // Act
            string result = IntakeSubmissionHelper.ExtractOidcSub(submission);

            // Assert
            result.ShouldBe("VERY-LONG-USER-IDENTIFIER-WITH-MANY-CHARACTERS-1234567890");
        }

        [Fact]
        public void ExtractOidcSub_FirstPathHasValue_DoesNotCheckSecondPath()
        {
            // Arrange - First path has valid value, second path should be ignored
            dynamic submission = new ExpandoObject();
            submission.submission = new ExpandoObject();
            submission.submission.data = new ExpandoObject();
            submission.submission.data.applicantAgent = new ExpandoObject();
            submission.submission.data.applicantAgent.sub = "first-path@idir";
            submission.createdBy = "second-path@bceid";

            // Act
            string result = IntakeSubmissionHelper.ExtractOidcSub(submission);

            // Assert - Should use first path value
            result.ShouldBe("FIRST-PATH");
        }
    }
}

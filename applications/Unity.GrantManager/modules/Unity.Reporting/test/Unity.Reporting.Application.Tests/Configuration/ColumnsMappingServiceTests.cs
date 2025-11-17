using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Reporting.Configuration;
using Unity.Reporting.Configuration.FieldsProviders;
using Unity.Reporting.Domain.Configuration;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Xunit;
using Xunit.Abstractions;

namespace Unity.Reporting.Application.Tests.Configuration
{
    public class ReportMappingServiceTests : ReportingApplicationTestBase<ReportingApplicationTestModule>
    {
        private readonly ReportMappingService _reportMappingService;
        private readonly IReportColumnsMapRepository _reportColumnsMapRepository;
        private readonly IBackgroundJobManager _backgroundJobManager;
        private readonly ICurrentTenant _currentTenant;
        private readonly IFieldsProvider _mockFieldsProvider;
        private readonly ILogger<ReportMappingService> _mockLogger;

        public ReportMappingServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {            
            _reportColumnsMapRepository = Substitute.For<IReportColumnsMapRepository>();
            _backgroundJobManager = Substitute.For<IBackgroundJobManager>();
            _currentTenant = Substitute.For<ICurrentTenant>();
            _mockLogger = Substitute.For<ILogger<ReportMappingService>>();

            // Create a mock fields provider for testing
            _mockFieldsProvider = Substitute.For<IFieldsProvider>();
            _mockFieldsProvider.CorrelationProvider.Returns("testProvider");

            var fieldsProviders = new List<IFieldsProvider> { _mockFieldsProvider };

            _reportMappingService = new ReportMappingService(_reportColumnsMapRepository,
                fieldsProviders,
                _backgroundJobManager,
                _currentTenant);

            // Set up the LazyServiceProvider and Logger using reflection
            SetupServicePropertiesForTesting(_reportMappingService, _mockLogger);
        }

        // This workaround allows us to inject these services whilst still mocking the other services
        // This case we dont make round trips to the MY-sql db for the tests
        private static void SetupServicePropertiesForTesting(object service, ILogger logger)
        {
            // Create a mock LazyServiceProvider that returns our mock logger
            var mockLazyServiceProvider = Substitute.For<IAbpLazyServiceProvider>();
            mockLazyServiceProvider.LazyGetService<ILogger>(Arg.Any<Func<IServiceProvider, ILogger>>())
                .Returns(logger);

            // Set the LazyServiceProvider property using reflection
            var lazyServiceProviderProperty = typeof(ReportMappingService)
                .GetProperty("LazyServiceProvider", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (lazyServiceProviderProperty == null)
            {
                // Try base classes
                var currentType = typeof(ReportMappingService).BaseType;
                while (currentType != null && lazyServiceProviderProperty == null)
                {
                    lazyServiceProviderProperty = currentType.GetProperty("LazyServiceProvider",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    currentType = currentType.BaseType;
                }
            }

            if (lazyServiceProviderProperty != null && lazyServiceProviderProperty.CanWrite)
            {
                lazyServiceProviderProperty.SetValue(service, mockLazyServiceProvider);
            }
            else
            {
                throw new InvalidOperationException("Could not find or set LazyServiceProvider property");
            }
        }

        // Helper method to convert array of field names to Dictionary<string, string>
        // where key is "key_{index}" and value is the field name
        private static Dictionary<string, string> ConvertToMap(string[] fields)
        {
            var result = new Dictionary<string, string>();
            for (int i = 0; i < fields.Length; i++)
            {
                result[$"key_{i}"] = fields[i];
            }
            return result;
        }

        // Helper method to call internal ValidateColumnNamesConformance method via reflection
        private static bool CallValidateColumnNamesConformance(UpsertMapRowDto[]? rows)
        {
            var type = typeof(ReportMappingUtils);
            var method = type.GetMethod("ValidateColumnNamesConformance", BindingFlags.NonPublic | BindingFlags.Static);
            return (bool)method!.Invoke(null, [rows])!;
        }

        [Fact]
        public void GenerateColumnNames_Should_Handle_Basic_Field_Names()
        {
            // Arrange
            var fields = new[] { "First Name", "Last Name", "Email" };

            // Act
            var result = _reportMappingService.GenerateColumnNames(ConvertToMap(fields));

            // Assert
            result["key_0"].ShouldBe("first_name");
            result["key_1"].ShouldBe("last_name");
            result["key_2"].ShouldBe("email");
        }

        [Fact]
        public void GenerateColumnNames_Should_Handle_Special_Characters()
        {
            // Arrange
            var fields = new[] { "Email@Address", "Phone#Number", "Date/Time", "Amount$Value" };

            // Act
            var result = _reportMappingService.GenerateColumnNames(ConvertToMap(fields));

            // Assert
            result["key_0"].ShouldBe("emailaddress");
            result["key_1"].ShouldBe("phonenumber");
            result["key_2"].ShouldBe("datetime");
            result["key_3"].ShouldBe("amountvalue");
        }

        [Fact]
        public void GenerateColumnNames_Should_Handle_Long_Field_Names()
        {
            // Arrange
            var longName = "This is a very long field name that exceeds the PostgreSQL column name limit";
            var fields = new[] { longName };

            // Act
            var result = _reportMappingService.GenerateColumnNames(ConvertToMap(fields));

            // Assert
            var columnName = result["key_0"];
            columnName.Length.ShouldBeLessThanOrEqualTo(60);
            columnName.ShouldNotEndWith("_");
            columnName.ShouldStartWith("this_is_a_very_long_field_name");
        }

        [Fact]
        public void GenerateColumnNames_Should_Handle_Names_Starting_With_Numbers()
        {
            // Arrange
            var fields = new[] { "123Field", "456_Column" };

            // Act
            var result = _reportMappingService.GenerateColumnNames(ConvertToMap(fields));

            // Assert
            result["key_0"].ShouldStartWith("col_");
            result["key_1"].ShouldStartWith("col_");
        }

        [Fact]
        public void GenerateColumnNames_Should_Ensure_Uniqueness()
        {
            // Arrange - Use different fields that result in same column name after sanitization
            var fields = new[] { "Name", "Name!", "Name?" };

            // Act
            var result = _reportMappingService.GenerateColumnNames(ConvertToMap(fields));

            // Assert
            result.Count.ShouldBe(3);
            result.Values.Distinct().Count().ShouldBe(3); // All values should be unique
            result["key_0"].ShouldBe("name");
            result["key_1"].ShouldBe("name_1");
            result["key_2"].ShouldBe("name_2");
        }

        [Fact]
        public void GenerateColumnNames_Should_Handle_Empty_And_Whitespace()
        {
            // Arrange - Use different whitespace patterns 
            var fields = new string[] { "", "   ", "  \t  " };

            // Act
            var result = _reportMappingService.GenerateColumnNames(ConvertToMap(fields));

            // Assert
            result["key_0"].ShouldBe("col_1");
            result["key_1"].ShouldBe("col_1_1");
            result["key_2"].ShouldBe("col_1_2");
        }

        [Fact]
        public void GenerateColumnNames_Should_Handle_Multiple_Spaces_And_Hyphens()
        {
            // Arrange
            var fields = new[] { "First   Name", "Last--Name", "Middle - - Name" };

            // Act
            var result = _reportMappingService.GenerateColumnNames(ConvertToMap(fields));

            // Assert
            result["key_0"].ShouldBe("first_name");
            result["key_1"].ShouldBe("last_name");
            result["key_2"].ShouldBe("middle_name");
        }

        [Fact]
        public void GenerateColumnNames_Should_Handle_Case_Insensitive_Duplicates()
        {
            // Arrange - Use different fields that become case-insensitive duplicates
            var fields = new[] { "Name", "NAME!", "name@", "Name#" };

            // Act
            var result = _reportMappingService.GenerateColumnNames(ConvertToMap(fields));

            // Assert
            result.Count.ShouldBe(4);
            result.Values.Distinct().Count().ShouldBe(4); // All values should be unique
            result["key_0"].ShouldBe("name");
            result["key_1"].ShouldBe("name_1");
            result["key_2"].ShouldBe("name_2");
            result["key_3"].ShouldBe("name_3");
        }

        [Fact]
        public void GenerateColumnNames_Should_Handle_PostgreSQL_Reserved_Words()
        {
            // Arrange - Using some PostgreSQL reserved words
            var fields = new[] { "user", "table", "select", "where" };

            // Act
            var result = _reportMappingService.GenerateColumnNames(ConvertToMap(fields));

            // Assert
            result["key_0"].ShouldBe("user");
            result["key_1"].ShouldBe("table");
            result["key_2"].ShouldBe("select");
            result["key_3"].ShouldBe("where");
        }

        [Fact]
        public void GenerateColumnNames_Should_Handle_Underscore_Edge_Cases()
        {
            // Arrange
            var fields = new[] { "_StartUnderscore", "EndUnderscore_", "___MultipleUnderscores___" };

            // Act
            var result = _reportMappingService.GenerateColumnNames(ConvertToMap(fields));

            // Assert
            result["key_0"].ShouldBe("startunderscore");
            result["key_1"].ShouldBe("endunderscore");
            result["key_2"].ShouldBe("multipleunderscores");
        }

        [Fact]
        public void GenerateColumnNames_Should_Handle_Mixed_Complex_Scenarios()
        {
            // Arrange - Complex real-world scenarios
            var fields = new[] {
                "Applicant's Full Name & Title",
                "2023-Q4 Revenue ($CAD)",
                "E-mail Address (Primary)",
                "Phone # - Business",
                "Date/Time Created"
            };

            // Act
            var result = _reportMappingService.GenerateColumnNames(ConvertToMap(fields));

            // Assert
            result["key_0"].ShouldBe("applicants_full_name_title");
            result["key_1"].ShouldBe("col_2023_q4_revenue_cad");
            result["key_2"].ShouldBe("e_mail_address_primary");
            result["key_3"].ShouldBe("phone_business");
            result["key_4"].ShouldBe("datetime_created");
        }

        [Fact]
        public void GenerateColumnNamesAsync_Should_Handle_Very_Long_Names_With_Uniqueness()
        {
            // Arrange - Test uniqueness with very long names that get truncated
            var baseLongName = "This is an extremely long field name that will definitely exceed sixty characters";
            var fields = new[] { baseLongName, baseLongName + "1", baseLongName + "2" };

            // Act
            var result = _reportMappingService.GenerateColumnNames(ConvertToMap(fields));

            // Assert
            result.Count.ShouldBe(3);
            result.Values.Distinct().Count().ShouldBe(3); // All should be unique even after truncation

            foreach (var value in result.Values)
            {
                value.Length.ShouldBeLessThanOrEqualTo(60);
            }
        }

        #region Backend Validation Tests

        [Fact]
        public void ValidateColumnNamesConformance_Should_Accept_Valid_Column_Names()
        {
            // Arrange
            var rows = new[]
            {
                new UpsertMapRowDto { PropertyName = "field1", ColumnName = "valid_column_name" },
                new UpsertMapRowDto { PropertyName = "field2", ColumnName = "another_valid_name" },
                new UpsertMapRowDto { PropertyName = "field3", ColumnName = "column_123" }
            };

            // Act & Assert
            var result = CallValidateColumnNamesConformance(rows);
            result.ShouldBeTrue();
        }

        [Fact]
        public void ValidateColumnNamesConformance_Should_Reject_Reserved_Words()
        {
            // Arrange
            var rows = new[]
            {
                new UpsertMapRowDto { PropertyName = "field1", ColumnName = "select" },
                new UpsertMapRowDto { PropertyName = "field2", ColumnName = "valid_name" }
            };

            // Act & Assert
            var result = CallValidateColumnNamesConformance(rows);
            result.ShouldBeFalse();
        }

        [Fact]
        public void ValidateColumnNamesConformance_Should_Reject_Names_Starting_With_Numbers()
        {
            // Arrange
            var rows = new[]
            {
                new UpsertMapRowDto { PropertyName = "field1", ColumnName = "123invalid" },
                new UpsertMapRowDto { PropertyName = "field2", ColumnName = "valid_name" }
            };

            // Act & Assert
            var result = CallValidateColumnNamesConformance(rows);
            result.ShouldBeFalse();
        }

        [Fact]
        public void ValidateColumnNamesConformance_Should_Reject_Names_With_Invalid_Characters()
        {
            // Arrange
            var rows = new[]
            {
                new UpsertMapRowDto { PropertyName = "field1", ColumnName = "invalid-name" },
                new UpsertMapRowDto { PropertyName = "field2", ColumnName = "invalid@name" },
                new UpsertMapRowDto { PropertyName = "field3", ColumnName = "invalid name" }
            };

            // Act & Assert
            var result = CallValidateColumnNamesConformance(rows);
            result.ShouldBeFalse();
        }

        [Fact]
        public void ValidateColumnNamesConformance_Should_Reject_Names_Too_Long()
        {
            // Arrange
            var longName = new string('a', 61); // 61 characters - exceeds limit
            var rows = new[]
            {
                new UpsertMapRowDto { PropertyName = "field1", ColumnName = longName }
            };

            // Act & Assert
            var result = CallValidateColumnNamesConformance(rows);
            result.ShouldBeFalse();
        }

        [Fact]
        public void ValidateColumnNamesConformance_Should_Accept_Names_At_Max_Length()
        {
            // Arrange
            var maxLengthName = new string('a', 60); // Exactly 60 characters
            var rows = new[]
            {
                new UpsertMapRowDto { PropertyName = "field1", ColumnName = maxLengthName }
            };

            // Act & Assert
            var result = CallValidateColumnNamesConformance(rows);
            result.ShouldBeTrue();
        }

        [Fact]
        public void ValidateColumnNamesConformance_Should_Allow_Empty_Column_Names()
        {
            // Arrange
            var rows = new[]
            {
                new UpsertMapRowDto { PropertyName = "field1", ColumnName = "" },
                new UpsertMapRowDto { PropertyName = "field2", ColumnName = " " },
                new UpsertMapRowDto { PropertyName = "field3", ColumnName = "   " }
            };

            // Act & Assert
            var result = CallValidateColumnNamesConformance(rows);
            result.ShouldBeTrue();
        }

        [Fact]
        public void ValidateColumnNamesConformance_Should_Handle_Case_Insensitive_Reserved_Words()
        {
            // Arrange
            var rows = new[]
            {
                new UpsertMapRowDto { PropertyName = "field1", ColumnName = "SELECT" },
                new UpsertMapRowDto { PropertyName = "field2", ColumnName = "Table" },
                new UpsertMapRowDto { PropertyName = "field3", ColumnName = "WHERE" }
            };

            // Act & Assert
            var result = CallValidateColumnNamesConformance(rows);
            result.ShouldBeFalse();
        }

        [Fact]
        public void ValidateColumnNamesConformance_Should_Accept_Null_Or_Empty_Array()
        {
            // Act & Assert
            CallValidateColumnNamesConformance(null).ShouldBeTrue();
            CallValidateColumnNamesConformance([]).ShouldBeTrue();
        }

        #endregion

        #region View Generation Tests

        [Fact]
        public async Task IsViewNameAvailableAsync_Should_Return_False_For_Null_Or_Empty_Name()
        {
            // Arrange & Act & Assert
            var resultNull = await _reportMappingService.IsViewNameAvailableAsync(null!);
            var resultEmpty = await _reportMappingService.IsViewNameAvailableAsync("");
            var resultWhitespace = await _reportMappingService.IsViewNameAvailableAsync("   ");

            resultNull.ShouldBeFalse();
            resultEmpty.ShouldBeFalse();
            resultWhitespace.ShouldBeFalse();
        }

        [Fact]
        public async Task IsViewNameAvailableAsync_Should_Return_True_When_View_Does_Not_Exist()
        {
            // Arrange
            var viewName = "test_view_123";
            _reportColumnsMapRepository.ViewExistsAsync(viewName).Returns(false);

            // Act
            var result = await _reportMappingService.IsViewNameAvailableAsync(viewName);

            // Assert
            result.ShouldBeTrue();
            await _reportColumnsMapRepository.Received(1).ViewExistsAsync(viewName);
        }

        [Fact]
        public async Task IsViewNameAvailableAsync_Should_Return_False_When_View_Exists()
        {
            // Arrange
            var viewName = "existing_view";
            _reportColumnsMapRepository.ViewExistsAsync(viewName).Returns(true);

            // Act
            var result = await _reportMappingService.IsViewNameAvailableAsync(viewName);

            // Assert
            result.ShouldBeFalse();
            await _reportColumnsMapRepository.Received(1).ViewExistsAsync(viewName);
        }

        [Fact]
        public async Task GenerateViewAsync_Should_Return_Success_Message()
        {
            // Arrange
            var correlationId = System.Guid.NewGuid();
            var correlationProvider = "testProvider";
            var viewName = "test_view";

            // Setup mocks
            _reportColumnsMapRepository.ViewExistsAsync(viewName).Returns(false);
            _reportColumnsMapRepository.FindByCorrelationAsync(correlationId, correlationProvider)
                .Returns(new ReportColumnsMap
                {
                    CorrelationId = correlationId,
                    CorrelationProvider = correlationProvider,
                    Mapping = "{\"Rows\":[]}"
                });

            // Act
            var result = await _reportMappingService.GenerateViewAsync(correlationId, correlationProvider, viewName);

            // Assert
            result.ShouldNotBeNull();
            result.Message.ShouldContain(viewName);
            result.Message.ShouldContain("queued");
            result.ViewName.ShouldBe(viewName);
            result.IsQueued.ShouldBeTrue();
        }

        [Fact]
        public async Task GenerateViewAsync_Should_Use_Correlation_Aware_Availability_Check()
        {
            // Arrange
            var correlationId = System.Guid.NewGuid();
            var correlationProvider = "testProvider";
            var viewName = "test_view";

            // Setup mocks - simulate view name is available for this correlation
            _reportColumnsMapRepository.FindByViewNameAsync(viewName).Returns((ReportColumnsMap?)null);
            _reportColumnsMapRepository.FindByCorrelationAsync(correlationId, correlationProvider)
                .Returns(new ReportColumnsMap
                {
                    CorrelationId = correlationId,
                    CorrelationProvider = correlationProvider,
                    Mapping = "{\"Rows\":[]}"
                });

            // Act
            var result = await _reportMappingService.GenerateViewAsync(correlationId, correlationProvider, viewName);

            // Assert
            result.ShouldNotBeNull();
            result.Message.ShouldContain(viewName);
            result.Message.ShouldContain("queued");
            result.ViewName.ShouldBe(viewName);
            result.IsQueued.ShouldBeTrue();
        }

        #endregion

        #region View Data Tests

        [Fact]
        public async Task ViewExistsAsync_Should_Delegate_To_Repository()
        {
            // Arrange
            var viewName = "test_view";
            _reportColumnsMapRepository.ViewExistsAsync(viewName).Returns(true);

            // Act
            var result = await _reportMappingService.ViewExistsAsync(viewName);

            // Assert
            result.ShouldBeTrue();
            await _reportColumnsMapRepository.Received(1).ViewExistsAsync(viewName);
        }

        [Fact]
        public async Task GetViewColumnNamesAsync_Should_Throw_For_Invalid_View_Name()
        {
            // Arrange
            _reportColumnsMapRepository.ViewExistsAsync(Arg.Any<string>()).Returns(false);

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(async () =>
                await _reportMappingService.GetViewColumnNamesAsync("nonexistent_view"));
        }

        [Fact]
        public async Task GetViewDataAsync_Should_Throw_For_Invalid_View_Name()
        {
            // Arrange
            _reportColumnsMapRepository.ViewExistsAsync(Arg.Any<string>()).Returns(false);
            var request = new ViewDataRequest { Skip = 0, Take = 10 };

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(async () =>
                await _reportMappingService.GetViewDataAsync("nonexistent_view", request));
        }

        [Fact]
        public async Task GetViewDataAsync_Should_Delegate_To_Repository_When_View_Exists()
        {
            // Arrange
            var viewName = "test_view";
            var request = new ViewDataRequest { Skip = 0, Take = 10 };
            var expectedResult = new ViewDataResult
            {
                Data = [],
                TotalCount = 0,
                ColumnNames = ["col1", "col2"]
            };

            _reportColumnsMapRepository.ViewExistsAsync(viewName).Returns(true);
            _reportColumnsMapRepository.GetViewDataAsync(viewName, request).Returns(expectedResult);

            // Act
            var result = await _reportMappingService.GetViewDataAsync(viewName, request);

            // Assert
            result.ShouldBe(expectedResult);
            await _reportColumnsMapRepository.Received(1).GetViewDataAsync(viewName, request);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task DeleteAsync_Should_Delete_Mapping_And_View()
        {
            // Arrange
            var correlationId = Guid.NewGuid();
            var correlationProvider = "testProvider";
            var viewName = "test_view";

            var reportColumnsMap = new ReportColumnsMap
            {
                CorrelationId = correlationId,
                CorrelationProvider = correlationProvider,
                Mapping = "{\"Rows\":}",

                ViewName = viewName
            };

            _reportColumnsMapRepository.FindByCorrelationAsync(correlationId, correlationProvider)
                .Returns(reportColumnsMap);

            _reportColumnsMapRepository.ViewExistsAsync(viewName).Returns(true);

            // Act
            await _reportMappingService.DeleteAsync(correlationId, correlationProvider, true);

            // Assert
            await _reportColumnsMapRepository.Received(1).ViewExistsAsync(viewName);
            await _reportColumnsMapRepository.Received(1).DeleteViewAsync(viewName);
            await _reportColumnsMapRepository.Received(1).DeleteAsync(reportColumnsMap);
        }

        [Fact]
        public async Task DeleteAsync_Should_Delete_Mapping_Without_View_When_DeleteView_False()
        {
            // Arrange
            var correlationId = Guid.NewGuid();
            var correlationProvider = "testProvider";
            var viewName = "test_view";

            var reportColumnsMap = new ReportColumnsMap
            {
                CorrelationId = correlationId,
                CorrelationProvider = correlationProvider,
                Mapping = "{\"Rows\":}",

                ViewName = viewName
            };

            _reportColumnsMapRepository.FindByCorrelationAsync(correlationId, correlationProvider)
                .Returns(reportColumnsMap);

            // Act
            await _reportMappingService.DeleteAsync(correlationId, correlationProvider, false);

            // Assert
            await _reportColumnsMapRepository.DidNotReceive().ViewExistsAsync(Arg.Any<string>());
            await _reportColumnsMapRepository.DidNotReceive().DeleteViewAsync(Arg.Any<string>());
            await _reportColumnsMapRepository.Received(1).DeleteAsync(reportColumnsMap);
        }

        [Fact]
        public async Task DeleteAsync_Should_Continue_When_View_Deletion_Fails()
        {
            // Arrange
            var correlationId = Guid.NewGuid();
            var correlationProvider = "testProvider";
            var viewName = "test_view";

            var reportColumnsMap = new ReportColumnsMap
            {
                CorrelationId = correlationId,
                CorrelationProvider = correlationProvider,
                Mapping = "{\"Rows\":[]}",
                ViewName = viewName
            };

            _reportColumnsMapRepository.FindByCorrelationAsync(correlationId, correlationProvider)
                .Returns(reportColumnsMap);

            _reportColumnsMapRepository.ViewExistsAsync(viewName).Returns(true);
            _reportColumnsMapRepository.When(x => x.DeleteViewAsync(viewName))
                .Do(x => throw new Exception("View deletion failed"));

            // Act & Assert - Should not throw exception
            await _reportMappingService.DeleteAsync(correlationId, correlationProvider, true);

            // Assert - Mapping should still be deleted even if view deletion fails
            await _reportColumnsMapRepository.Received(1).DeleteViewAsync(viewName);
            await _reportColumnsMapRepository.Received(1).DeleteAsync(reportColumnsMap);
        }

        [Fact]
        public async Task DeleteAsync_Should_Throw_EntityNotFoundException_When_Mapping_Not_Found()
        {
            // Arrange
            var correlationId = Guid.NewGuid();
            var correlationProvider = "testProvider";

            _reportColumnsMapRepository.FindByCorrelationAsync(correlationId, correlationProvider)
                .Returns((ReportColumnsMap?)null);

            // Act & Assert
            await Should.ThrowAsync<Volo.Abp.Domain.Entities.EntityNotFoundException>(async () =>
                await _reportMappingService.DeleteAsync(correlationId, correlationProvider, true));
        }

        [Fact]
        public async Task DeleteAsync_Should_Throw_ArgumentException_For_Invalid_Provider()
        {
            // Arrange
            var correlationId = Guid.NewGuid();
            var invalidProvider = "invalid_provider";

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(async () =>
                await _reportMappingService.DeleteAsync(correlationId, invalidProvider, true));
        }

        [Fact]
        public async Task DeleteAsync_Should_Skip_View_Deletion_When_ViewName_Is_Empty()
        {
            // Arrange
            var correlationId = Guid.NewGuid();
            var correlationProvider = "testProvider";

            var reportColumnsMap = new ReportColumnsMap
            {
                CorrelationId = correlationId,
                CorrelationProvider = correlationProvider,
                Mapping = "{\"Rows\":[]}",

                ViewName = "" // Empty view name
            };

            _reportColumnsMapRepository.FindByCorrelationAsync(correlationId, correlationProvider)
                .Returns(reportColumnsMap);

            // Act
            await _reportMappingService.DeleteAsync(correlationId, correlationProvider, true);

            // Assert - Should not attempt view operations
            await _reportColumnsMapRepository.DidNotReceive().ViewExistsAsync(Arg.Any<string>());
            await _reportColumnsMapRepository.DidNotReceive().DeleteViewAsync(Arg.Any<string>());
            await _reportColumnsMapRepository.Received(1).DeleteAsync(reportColumnsMap);
        }

        #endregion
    }
}
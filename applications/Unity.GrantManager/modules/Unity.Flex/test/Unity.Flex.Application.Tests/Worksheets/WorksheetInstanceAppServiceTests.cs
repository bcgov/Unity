using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.WorksheetInstances;
using Xunit;
using Xunit.Abstractions;

namespace Unity.Flex.Worksheets
{
    public class WorksheetInstanceAppServiceTests : FlexApplicationTestBase
    {
        private readonly IWorksheetInstanceAppService _worksheetInstanceAppService;
        private readonly IWorksheetInstanceRepository _worksheetInstanceRepository;

        public WorksheetInstanceAppServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _worksheetInstanceAppService = GetRequiredService<IWorksheetInstanceAppService>();
            _worksheetInstanceRepository = GetRequiredService<IWorksheetInstanceRepository>();
        }


        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetWorksheetInstanceByCorrelationAnchor_WorksheetProvided()
        {
            // Arrange
            var correlationId = Guid.NewGuid();
            var correlationProvider = "Application";
            var worksheetCorrelationId = Guid.NewGuid();
            var worksheetCorrelationProvider = "FormVersion";
            var worksheetId = Guid.NewGuid();
            var correlationAnchor = "UiAnchor";

            await _worksheetInstanceRepository.InsertAsync(new WorksheetInstance(Guid.NewGuid(),
                worksheetId,
                correlationId,
                correlationProvider,
                worksheetCorrelationId,
                worksheetCorrelationProvider,
                correlationAnchor), true);

            // Act
            var instance = await _worksheetInstanceAppService
                .GetByCorrelationAnchorAsync(correlationId, correlationProvider, worksheetId, correlationAnchor);


            // Assert
            instance.ShouldNotBeNull();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetWorksheetInstanceByCorrelationAnchor_WorksheetNull()
        {
            var correlationId = Guid.NewGuid();
            var correlationProvider = "Application";
            var worksheetCorrelationId = Guid.NewGuid();
            var worksheetCorrelationProvider = "FormVersion";
            var worksheetId = Guid.NewGuid();
            var correlationAnchor = "UiAnchor";

            await _worksheetInstanceRepository.InsertAsync(new WorksheetInstance(Guid.NewGuid(),
                worksheetId,
                correlationId,
                correlationProvider,
                worksheetCorrelationId,
                worksheetCorrelationProvider,
                correlationAnchor), true);

            // Act
            var instance = await _worksheetInstanceAppService
                .GetByCorrelationAnchorAsync(correlationId, correlationProvider, null, correlationAnchor);


            // Assert
            instance.ShouldNotBeNull();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task CreateWorksheetInstance()
        {
            // Arrange
            var correlationId = Guid.NewGuid();
            var correlationProvider = "Application";
            var worksheetCorrelationId = Guid.NewGuid();
            var worksheetCorrelationProvider = "FormVersion";
            var worksheetId = Guid.NewGuid();
            var correlationAnchor = "UiAnchor";

            // Act
            var worksheetInstance = await _worksheetInstanceAppService
                .CreateAsync(new CreateWorksheetInstanceDto()
                {
                    CorrelationId = correlationId,
                    CorrelationAnchor = correlationAnchor,
                    CorrelationProvider = correlationProvider,
                    SheetCorrelationId = worksheetCorrelationId,
                    SheetCorrelationProvider = worksheetCorrelationProvider,
                    WorksheetId = worksheetId
                });

            // Assert
            var instance = await _worksheetInstanceRepository.GetAsync(worksheetInstance.Id);
            instance.ShouldNotBeNull();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task UpdateWorksheetInstance()
        {
            // Arrange
            var correlationId = Guid.NewGuid();
            var correlationProvider = "Application";
            var worksheetCorrelationId = Guid.NewGuid();
            var worksheetCorrelationProvider = "FormVersion";
            var worksheetId = Guid.NewGuid();
            var correlationAnchor = "UiAnchor";

            await _worksheetInstanceRepository.InsertAsync(new WorksheetInstance(Guid.NewGuid(),
                worksheetId,
                correlationId,
                correlationProvider,
                worksheetCorrelationId,
                worksheetCorrelationProvider,
                correlationAnchor), true);

            // Act and Assert
            await _worksheetInstanceAppService.UpdateAsync(new PersistWorksheetIntanceValuesDto()
            {
                InstanceCorrelationId = correlationId,
                InstanceCorrelationProvider = correlationProvider,
                FormDataName = "Form",
                SheetCorrelationId = worksheetCorrelationId,
                SheetCorrelationProvider = worksheetCorrelationProvider,
                UiAnchor = correlationAnchor,
                VersionId = Guid.NewGuid(),
                WorksheetId = worksheetId
            }).ShouldNotThrowAsync();
        }
    }
}
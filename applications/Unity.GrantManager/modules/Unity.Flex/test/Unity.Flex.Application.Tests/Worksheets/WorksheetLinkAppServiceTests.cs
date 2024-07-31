using System.Threading.Tasks;
using System;
using Unity.Flex.Domain.Worksheets;
using Xunit;
using Xunit.Abstractions;
using Unity.Flex.WorksheetLinks;
using Shouldly;
using Unity.Flex.Domain.WorksheetLinks;

namespace Unity.Flex.Worksheets
{
    public class WorksheetLinkAppServiceTests : FlexApplicationTestBase
    {
        private readonly IWorksheetLinkAppService _worksheetLinkAppService;
        private readonly IWorksheetRepository _worksheetRepository;
        private readonly IWorksheetLinkRepository _worksheetLinkRepository;

        public WorksheetLinkAppServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _worksheetRepository = GetRequiredService<IWorksheetRepository>();
            _worksheetLinkAppService = GetRequiredService<IWorksheetLinkAppService>();
            _worksheetLinkRepository = GetRequiredService<IWorksheetLinkRepository>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task UpdateWorksheetLinks()
        {
            // Arrange
            var correlationProviderForm = "Form Version";
            var correlationProviderWs = "Application";
            var worksheetDb = await _worksheetRepository.InsertAsync(new Worksheet(Guid.NewGuid(), "Get", "Get Me"), true);
            var worksheetDb2 = await _worksheetRepository.InsertAsync(new Worksheet(Guid.NewGuid(), "Get2", "Get Me 2"), true);

            // Act
            _ = await _worksheetLinkAppService.UpdateWorksheetLinksAsync(new UpdateWorksheetLinksDto()
            {
                CorrelationId = Guid.NewGuid(),
                CorrelationProvider = correlationProviderForm,
                WorksheetAnchors = new System.Collections.Generic.Dictionary<Guid, string> {
                    { worksheetDb.Id, correlationProviderWs },
                    { worksheetDb2.Id, correlationProviderWs }
                }
            });

            // Assert
            var worksheetLinks = await _worksheetLinkRepository.GetListAsync();
            worksheetLinks.Count.ShouldBe(2);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task UpdateWorksheetLinks_WithDeletion()
        {
            // Arrange
            var correlationId = Guid.NewGuid();
            var correlationProviderForm = "Form Version";
            var correlationProviderWs = "Application";
            var anchor = "UiAnchor";
            var worksheetDb = await _worksheetRepository.InsertAsync(new Worksheet(Guid.NewGuid(), "Get", "Get Me"), true);
            var worksheetDb2 = await _worksheetRepository.InsertAsync(new Worksheet(Guid.NewGuid(), "Get2", "Get Me 2"), true);
            await _worksheetLinkRepository.InsertAsync(new WorksheetLink(Guid.NewGuid(), worksheetDb.Id, correlationId, correlationProviderForm, anchor), true);
            await _worksheetLinkRepository.InsertAsync(new WorksheetLink(Guid.NewGuid(), worksheetDb2.Id, correlationId, correlationProviderForm, anchor), true);

            // Act

            _ = await _worksheetLinkAppService.UpdateWorksheetLinksAsync(new UpdateWorksheetLinksDto()
            {
                CorrelationId = correlationId,
                CorrelationProvider = correlationProviderForm,
                WorksheetAnchors = new System.Collections.Generic.Dictionary<Guid, string> {
                    { worksheetDb.Id, correlationProviderWs },                    
                }
            });

            // Assert
            var worksheetLinks = await _worksheetLinkRepository.GetListAsync();
            worksheetLinks.Count.ShouldBe(1);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetListByCorrelation()
        {
            // Arrange
            var correlationId = Guid.NewGuid();
            var correlationProvider = "Unit Test";
            var anchor = "Anchor";

            var worksheetDb = await _worksheetRepository.InsertAsync(new Worksheet(Guid.NewGuid(), "Get", "Get Me"), true);
            _ = await _worksheetLinkRepository.InsertAsync(new WorksheetLink(Guid.NewGuid(), worksheetDb.Id, correlationId, correlationProvider, anchor), true);

            // Act
            var worksheets = await _worksheetLinkAppService.GetListByCorrelationAsync(correlationId, correlationProvider);

            // Assert
            worksheets.Count.ShouldBe(1);
            worksheets[0].Worksheet.Title = "Get Me";
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetListByWorksheet()
        {
            // Arrange
            var correlationId = Guid.NewGuid();
            var correlationProvider = "Unit Test";
            var anchor = "Anchor";

            var worksheetDb = await _worksheetRepository.InsertAsync(new Worksheet(Guid.NewGuid(), "Get", "Get Me"), true);
            _ = await _worksheetLinkRepository.InsertAsync(new WorksheetLink(Guid.NewGuid(), worksheetDb.Id, correlationId, correlationProvider, anchor), true);

            // Act
            var worksheets = await _worksheetLinkAppService.GetListByWorksheetAsync(worksheetDb.Id, correlationProvider);

            // Assert
            worksheets.Count.ShouldBe(1);
            worksheets[0].Worksheet.Title = "Get Me";
        }
    }
}


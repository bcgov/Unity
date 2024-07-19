using System.Threading.Tasks;
using System;
using Unity.Flex.Domain.Worksheets;
using Xunit;
using Xunit.Abstractions;
using Shouldly;
using Unity.Flex.Domain.WorksheetLinks;

namespace Unity.Flex.Worksheets
{
    public class WorksheetListAppServiceTests : FlexApplicationTestBase
    {
        private readonly IWorksheetListAppService _worksheetListAppService;
        private readonly IWorksheetRepository _worksheetRepository;
        private readonly IWorksheetLinkRepository _worksheetLinkRepository;

        public WorksheetListAppServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _worksheetListAppService = GetRequiredService<IWorksheetListAppService>();
            _worksheetRepository = GetRequiredService<IWorksheetRepository>();
            _worksheetLinkRepository = GetRequiredService<IWorksheetLinkRepository>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetWorksheet()
        {
            // Arrange           
            var worksheetDb = await _worksheetRepository.InsertAsync(new Worksheet(Guid.NewGuid(), "Get", "Get Me"), true);

            // Act
            var worksheet = await _worksheetListAppService.GetAsync(worksheetDb.Id);

            // Assert
            worksheet.ShouldNotBeNull();
            worksheet.Title.ShouldBe("Get Me");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetWorksheetList()
        {
            // Arrange           
            var worksheetDb = await _worksheetRepository.InsertAsync(new Worksheet(Guid.NewGuid(), "Get", "Get Me"), true);

            // Act
            var worksheets = await _worksheetListAppService.GetListAsync();

            // Assert
            worksheets.Count.ShouldBe(1);
            worksheets[0].Title.ShouldBe("Get Me");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetWorksheetListByCorrelation_ShouldBeEmptyList()
        {
            // Arrange           
            var correlationId = Guid.NewGuid();
            var correlationProvider = "UnitTest";

            // Act
            var result = await _worksheetListAppService.GetListByCorrelationAsync(correlationId, correlationProvider);

            // Assert
            result.Count.ShouldBe(0);
        }


        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetWorksheetListByCorrelation()
        {
            // Arrange
            var correlationId = Guid.NewGuid();
            var correlationProvider = "UnitTest";
            var anchor = "UnitTest";

            var worksheetDb = await _worksheetRepository.InsertAsync(new Worksheet(Guid.NewGuid(), "Get", "Get Me"), true);
            _ = await _worksheetLinkRepository.InsertAsync(new WorksheetLink(Guid.NewGuid(), worksheetDb.Id, correlationId, correlationProvider, anchor), true);

            // Act
            var worksheets = await _worksheetListAppService.GetListByCorrelationAsync(correlationId, correlationProvider);

            // Assert
            worksheets.Count.ShouldBe(1);
            worksheets[0].Title.ShouldBe("Get Me");
        }
    }
}

using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.WorksheetInstances;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using Xunit;
using Xunit.Abstractions;

namespace Unity.Flex.Worksheets
{
    public class WorksheetInstanceRepositoryTests : FlexApplicationTestBase
    {
        private readonly IWorksheetInstanceRepository _worksheetInstanceRepository;
        private readonly ICurrentTenant _currentTenant;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public WorksheetInstanceRepositoryTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _worksheetInstanceRepository = GetRequiredService<IWorksheetInstanceRepository>();
            _currentTenant = GetRequiredService<ICurrentTenant>();
            _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetByCorrelationIdsAsync_OnlyReturnsRowsForTheExactCorrelationProviderRequested()
        {
            var correlationId = Guid.NewGuid();

            await _worksheetInstanceRepository.InsertAsync(new WorksheetInstance(
                Guid.NewGuid(), Guid.NewGuid(), correlationId, "Application",
                Guid.NewGuid(), "FormVersion", "anchor"), autoSave: true);
            await _worksheetInstanceRepository.InsertAsync(new WorksheetInstance(
                Guid.NewGuid(), Guid.NewGuid(), correlationId, "OtherProvider",
                Guid.NewGuid(), "FormVersion", "anchor"), autoSave: true);

            var result = await _worksheetInstanceRepository.GetByCorrelationIdsAsync([correlationId], "Application");

            result.ShouldHaveSingleItem();
            result[0].CorrelationProvider.ShouldBe("Application");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetByCorrelationIdsAsync_RespectsMultiTenantFilter()
        {
            var tenantA = Guid.NewGuid();
            var tenantB = Guid.NewGuid();
            var correlationId = Guid.NewGuid();

            using (_currentTenant.Change(tenantA))
            {
                using var uow = _unitOfWorkManager.Begin();
                await _worksheetInstanceRepository.InsertAsync(new WorksheetInstance(
                    Guid.NewGuid(), Guid.NewGuid(), correlationId, "Application",
                    Guid.NewGuid(), "FormVersion", "anchor"), autoSave: true);
                await uow.CompleteAsync();
            }

            using (_currentTenant.Change(tenantA))
            {
                using var uow = _unitOfWorkManager.Begin();
                var visibleToOwnTenant = await _worksheetInstanceRepository.GetByCorrelationIdsAsync([correlationId], "Application");
                visibleToOwnTenant.ShouldHaveSingleItem();
            }

            using (_currentTenant.Change(tenantB))
            {
                using var uow = _unitOfWorkManager.Begin();
                var visibleToOtherTenant = await _worksheetInstanceRepository.GetByCorrelationIdsAsync([correlationId], "Application");
                visibleToOtherTenant.ShouldBeEmpty();
            }
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetDistinctWorksheetIdsByCorrelationIdsAsync_OnlyReturnsIdsForTheExactCorrelationProviderRequested()
        {
            var correlationId = Guid.NewGuid();
            var worksheetIdForRequestedProvider = Guid.NewGuid();
            var worksheetIdForOtherProvider = Guid.NewGuid();

            await _worksheetInstanceRepository.InsertAsync(new WorksheetInstance(
                Guid.NewGuid(), worksheetIdForRequestedProvider, correlationId, "Application",
                Guid.NewGuid(), "FormVersion", "anchor"), autoSave: true);
            await _worksheetInstanceRepository.InsertAsync(new WorksheetInstance(
                Guid.NewGuid(), worksheetIdForOtherProvider, correlationId, "OtherProvider",
                Guid.NewGuid(), "FormVersion", "anchor"), autoSave: true);

            var result = await _worksheetInstanceRepository.GetDistinctWorksheetIdsByCorrelationIdsAsync([correlationId], "Application");

            result.ShouldBe([worksheetIdForRequestedProvider]);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetPagedListByCorrelationProviderAsync_OnlyReturnsRowsForTheExactCorrelationProviderRequested()
        {
            var marker = Guid.NewGuid().ToString("N");
            var correlationProvider = $"Provider-{marker}";

            await _worksheetInstanceRepository.InsertAsync(new WorksheetInstance(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), correlationProvider,
                Guid.NewGuid(), "FormVersion", "anchor"), autoSave: true);
            await _worksheetInstanceRepository.InsertAsync(new WorksheetInstance(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "OtherProvider",
                Guid.NewGuid(), "FormVersion", "anchor"), autoSave: true);

            var result = await _worksheetInstanceRepository.GetPagedListByCorrelationProviderAsync(correlationProvider, 0, 10);

            result.ShouldHaveSingleItem();
            result[0].CorrelationProvider.ShouldBe(correlationProvider);
        }
    }
}

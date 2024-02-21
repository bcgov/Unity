using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Settings;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;
using Volo.Abp.Uow;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.Locality
{
    public class SectorAppServiceTests : GrantManagerApplicationTestBase
    {
        private readonly SectorAppService _sectorAppService;
        private readonly ISectorRepository _sectorRepository;
        private readonly ISubSectorRepository _subSectorRepository;
        private readonly ICurrentTenant _currentTenant;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly ISettingManager _settingManager;
        private readonly Guid sectorOne;
        private readonly Guid sectorTwo;
        private readonly Guid subSectorOne;
        private readonly Guid subSectorTwo;
        private readonly Guid tenantId;

        public class SectorSeed : Sector
        {
            public SectorSeed(Guid id)
            {
                Id = id;
            }
        }

        public class SubSectorSeed : SubSector
        {
            public SubSectorSeed(Guid id)
            {
                Id = id;
            }
        }

        public SectorAppServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _sectorAppService = GetRequiredService<SectorAppService>();
            _sectorRepository = GetRequiredService<ISectorRepository>();
            _currentTenant = GetRequiredService<ICurrentTenant>();
            _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
            _settingManager = GetRequiredService<ISettingManager>();
            _subSectorRepository = GetRequiredService<ISubSectorRepository>();

            sectorOne = Guid.NewGuid();
            sectorTwo = Guid.NewGuid();
            subSectorOne = Guid.NewGuid();
            subSectorTwo = Guid.NewGuid();
            tenantId = Guid.NewGuid();

            _currentTenant.Change(tenantId);
        }

        [Fact]
        public async Task GetListAsync_IsNotFiltered_WithNoFilterSet()
        {
            using var uow = _unitOfWorkManager.Begin();

            // Arrange
            await SeedSectors();
            await _settingManager.SetAsync(SettingsConstants.SectorFilterName, null, TenantSettingValueProvider.ProviderName, _currentTenant.Id.ToString());

            // Act            
            var list = await _sectorAppService.GetListAsync();

            // Assert
            list.Count.ShouldBe(2);
        }

        [Fact]
        public async Task GetListAsync_IsFiltered_WithFilterSet()
        {
            using var uow = _unitOfWorkManager.Begin();

            // Arrange
            await SeedSectors();
            await _settingManager.SetAsync(SettingsConstants.SectorFilterName, "SEC1", TenantSettingValueProvider.ProviderName, _currentTenant.Id.ToString());

            // Act
            var list = await _sectorAppService.GetListAsync();

            // Assert
            list.Count.ShouldBe(1);
        }

        [Fact]
        public async Task GetListAsync_HasSubSectors()
        {
            using var uow = _unitOfWorkManager.Begin();

            // Arrange
            await SeedSectors();

            // Act
            var list = await _sectorAppService.GetListAsync();

            // Assert
            list.Count.ShouldBe(2);
            var sectorTest = list.First(s => s.SectorName == "Sector1");
            sectorTest.SubSectors.ShouldNotBeNull();
            sectorTest.SubSectors!.Count.ShouldBe(2);
        }


        private async Task SeedSectors()
        {
            await _sectorRepository.InsertAsync(
                new SectorSeed(sectorOne)
                {
                    SectorCode = "SEC1",
                    SectorName = "Sector1",            
                }, true);

            await _subSectorRepository.InsertAsync(
                new SubSectorSeed(subSectorOne)
                {
                    SectorId = sectorOne,                    
                    SubSectorCode = "SEC1SUB1",
                    SubSectorName = "SEC1SUB1"
                }, true);

            await _subSectorRepository.InsertAsync(
               new SubSectorSeed(subSectorTwo)
               {
                   SectorId = sectorOne,
                   SubSectorCode = "SEC1SUB2",
                   SubSectorName = "SEC1SUB2"
               }, true);

            await _sectorRepository.InsertAsync(
              new SectorSeed(sectorTwo)
              {
                  SectorCode = "SEC2",
                  SectorName = "Sector2",
              }, true);
        }
    }
}

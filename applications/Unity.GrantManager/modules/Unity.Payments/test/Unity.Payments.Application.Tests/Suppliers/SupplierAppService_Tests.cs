using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Unity.Payments.Domain.Suppliers;
using Unity.GrantManager.Integrations; // for IEndpointManagementAppService
using Volo.Abp.Application.Dtos;
using Volo.Abp.Uow;
using Xunit;

namespace Unity.Payments.Suppliers
{
    public class SupplierAppService_Tests : PaymentsApplicationTestBase
    {
        private readonly ISupplierAppService _supplierAppService;
        private readonly ISupplierRepository _supplierRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public SupplierAppService_Tests()
        {
            // Before resolving anything, ensure the fake is registered.
            ConfigureTestServices();

            _supplierAppService = GetRequiredService<ISupplierAppService>();
            _supplierRepository = GetRequiredService<ISupplierRepository>();
            _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
        }

        /// <summary>
        /// Registers the stub implementation so SupplierService can resolve it.
        /// </summary>
        private void ConfigureTestServices()
        {
            var services = ServiceProvider.GetRequiredService<IServiceCollection>();
            services.AddSingleton<IEndpointManagementAppService, FakeEndpointManagementAppService>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task CreateAsync_CreatesSupplier()
        {
            // Arrange
            var createSupplierDto = new CreateSupplierDto
            {
                Name = "Supplier123",
                Number = "12345",
                CorrelationId = Guid.NewGuid(),
                CorrelationProvider = "Applicant",
                MailingAddress = "123 Goldstream Ave.",
                City = "Langford",
                Province = "BC",
                PostalCode = "12345",
            };

            // Act
            var supplier = await _supplierAppService.CreateAsync(createSupplierDto);

            // Assert
            var dbSupplier = await _supplierRepository.GetAsync(supplier.Id);
            Assert.Equal(dbSupplier.Name, createSupplierDto.Name);
            Assert.Equal(dbSupplier.Number, createSupplierDto.Number);
            Assert.Equal(dbSupplier.MailingAddress, createSupplierDto.MailingAddress);
            Assert.Equal(dbSupplier.City, createSupplierDto.City);
            Assert.Equal(dbSupplier.Province, createSupplierDto.Province);
            Assert.Equal(dbSupplier.PostalCode, createSupplierDto.PostalCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task UpdateAsync_UpdatesSupplier()
        {
            // Arrange
            var updateSupplierDto = new UpdateSupplierDto
            {
                Name = "Supplier456",
                Number = "67890",
                Subcategory = "",
                ProviderId = "",
                BusinessNumber = "",
                Status = "",
                SupplierProtected = "",
                StandardIndustryClassification = "",
                LastUpdatedInCAS = DateTime.Now,
                MailingAddress = "890 Peatt Road",
                City = "Langford",
                Province = "BC",
                PostalCode = "67890",
            };

            var createSupplierDto = new CreateSupplierDto
            {
                Name = "Supplier123",
                Number = "12345",
                CorrelationId = Guid.NewGuid(),
                CorrelationProvider = "Applicant",
                MailingAddress = "123 Goldstream Ave.",
                City = "Langford",
                Province = "BC",
                PostalCode = "12345",
            };

            using var uow = _unitOfWorkManager.Begin();

            // Act
            var supplier = await _supplierAppService.CreateAsync(createSupplierDto);
            var updatedSupplier = await _supplierAppService.UpdateAsync(supplier.Id, updateSupplierDto);

            // Assert
            var dbSupplier = await _supplierRepository.GetAsync(supplier.Id);
            Assert.Equal(dbSupplier.Name, updateSupplierDto.Name);
            Assert.Equal(dbSupplier.Number, updateSupplierDto.Number);
            Assert.Equal(dbSupplier.MailingAddress, updateSupplierDto.MailingAddress);
            Assert.Equal(dbSupplier.City, updateSupplierDto.City);
            Assert.Equal(dbSupplier.Province, updateSupplierDto.Province);
            Assert.Equal(dbSupplier.PostalCode, updateSupplierDto.PostalCode);
        }
    }
    /// <summary>
    /// Minimal stub for IEndpointManagementAppService so Autofac can resolve dependencies.
    /// </summary>
    public class FakeEndpointManagementAppService : IEndpointManagementAppService
    {
        public Task<string> GetEndpointAsync(string key)
        {
            // Return a dummy endpoint for test purposes
            return Task.FromResult("https://fake-endpoint.local");
        }

        public Task<string> GetChefsApiBaseUrlAsync()
        {
            return Task.FromResult("https://fake-chefs-api.local");
        }

        public Task<string> GetUrlByKeyNameAsync(string keyName)
        {
            return Task.FromResult("https://fake-url.local");
        }

        public Task<string> GetUgmUrlByKeyNameAsync(string keyName)
        {
            return Task.FromResult("https://fake-ugm-url.local");
        }

        public Task ClearCacheAsync(Guid? id)
        {
            return Task.CompletedTask;
        }

        public Task<DynamicUrlDto> GetAsync(Guid id)
        {
            return Task.FromResult(new DynamicUrlDto());
        }

        public Task<PagedResultDto<DynamicUrlDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            return Task.FromResult(new PagedResultDto<DynamicUrlDto>());
        }

        public Task<DynamicUrlDto> CreateAsync(CreateUpdateDynamicUrlDto input)
        {
            return Task.FromResult(new DynamicUrlDto());
        }

        public Task<DynamicUrlDto> UpdateAsync(Guid id, CreateUpdateDynamicUrlDto input)
        {
            return Task.FromResult(new DynamicUrlDto());
        }

        public Task DeleteAsync(Guid id)
        {
            return Task.CompletedTask;
        }
    }

}

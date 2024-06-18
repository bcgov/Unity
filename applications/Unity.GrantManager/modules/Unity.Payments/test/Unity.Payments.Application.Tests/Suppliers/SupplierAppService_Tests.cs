using System;
using System.Threading.Tasks;
using Unity.Payments.Domain.Suppliers;
using Volo.Abp.Uow;
using Xunit;

namespace Unity.Payments.Suppliers;

public class SupplierAppService_Tests : PaymentsApplicationTestBase
{
    private readonly ISupplierAppService _supplierAppService;
    private readonly ISupplierRepository _supplierRepository;
    private readonly Volo.Abp.Uow.IUnitOfWorkManager _unitOfWorkManager;

    public SupplierAppService_Tests()
    {
        _supplierAppService = GetRequiredService<ISupplierAppService>();
        _supplierRepository = GetRequiredService<ISupplierRepository>();
        _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateAsync_CreatesSupplier()
    {
        // Arrange
        CreateSupplierDto createSupplierDto = new()
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
        SupplierDto supplier = await _supplierAppService.CreateAsync(createSupplierDto);

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
        var updateSupplierDto = new UpdateSupplierDto()
        {
            Name = "Supplier456",
            Number = "67890",
            Subcategory = "",
            ProviderId = "",
            BusinessNumber = "",
            Status = "",
            SupplierProtected = "",
            StandardIndustryClassification = "",
            LastUpdatedInCAS = System.DateTime.Now,
            MailingAddress = "890 Peatt Road",
            City = "Langford",
            Province = "BC",
            PostalCode = "67890",
        };
        
        CreateSupplierDto createSupplierDto = new()
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
        SupplierDto supplier = await _supplierAppService.CreateAsync(createSupplierDto);
        SupplierDto updatedSupDto = await _supplierAppService.UpdateAsync(supplier.Id, updateSupplierDto);

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

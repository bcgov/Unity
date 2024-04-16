using System;
using System.Threading.Tasks;
using Xunit;


namespace Unity.Payments.Suppliers;

public class SupplierAppService_Tests : PaymentsApplicationTestBase
{
    private readonly ISupplierAppService _supplierAppService;
    public SupplierAppService_Tests()
    {
        _supplierAppService = GetRequiredService<ISupplierAppService>();
    }

    [Fact]
    public async Task CreateAsync()
    {
        // Arrange
        CreateSupplierDto createSupplierDto = new CreateSupplierDto()
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
        Assert.NotNull(supplier);
        Assert.Equal(supplier.Name, createSupplierDto.Name);
        Assert.Equal(supplier.Number, createSupplierDto.Number);
        Assert.Equal(supplier.MailingAddress, createSupplierDto.MailingAddress);
        Assert.Equal(supplier.City, createSupplierDto.City);
        Assert.Equal(supplier.Province, createSupplierDto.Province);
        Assert.Equal(supplier.PostalCode, createSupplierDto.PostalCode);
    }

    [Fact]
    public async Task UpdateAsync()
    {
        // Arrange
        CreateSupplierDto createSupplierDto = new CreateSupplierDto()
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
        UpdateSupplierDto updateSupplierDto = new UpdateSupplierDto()
        {
            Name = "Supplier456",
            Number = "67890",
            MailingAddress = "890 Peatt Road",
            City = "Langford",
            Province = "BC",
            PostalCode = "67890",
        };
        SupplierDto insertedSupplier = await _supplierAppService.CreateAsync(createSupplierDto);


        // Act
        SupplierDto supplier = await _supplierAppService.UpdateAsync(insertedSupplier.Id,updateSupplierDto);

        // Assert
        Assert.NotNull(supplier);
        Assert.Equal(supplier.Name, updateSupplierDto.Name);
        Assert.Equal(supplier.Number, updateSupplierDto.Number);
        Assert.Equal(supplier.MailingAddress, updateSupplierDto.MailingAddress);
        Assert.Equal(supplier.City, updateSupplierDto.City);
        Assert.Equal(supplier.Province, updateSupplierDto.Province);
        Assert.Equal(supplier.PostalCode, updateSupplierDto.PostalCode);
    }
}

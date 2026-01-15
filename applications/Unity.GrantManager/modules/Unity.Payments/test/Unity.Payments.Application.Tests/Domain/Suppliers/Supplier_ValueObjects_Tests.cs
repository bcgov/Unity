using System;
using System.Threading.Tasks;
using Shouldly;
using Unity.Modules.Shared.Correlation;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Domain.Suppliers.ValueObjects;
using Xunit;
using System.ComponentModel;

namespace Unity.Payments.Domain.Suppliers;

[Category("Domain")]
public class Supplier_ValueObjects_Tests : PaymentsApplicationTestBase
{
    #region Value Object Creation Tests

    [Fact]
    public void SupplierBasicInfo_Create_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var name = "Test Supplier";
        var number = "SUP001";
        var subcategory = "Category A";

        // Act
        var basicInfo = new SupplierBasicInfo(name, number, subcategory);

        // Assert
        basicInfo.Name.ShouldBe(name);
        basicInfo.Number.ShouldBe(number);
        basicInfo.Subcategory.ShouldBe(subcategory);
    }

    [Fact]
    public void ProviderInfo_Create_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var providerId = "PROV123";
        var businessNumber = "BN123456789";

        // Act
        var providerInfo = new ProviderInfo(providerId, businessNumber);

        // Assert
        providerInfo.ProviderId.ShouldBe(providerId);
        providerInfo.BusinessNumber.ShouldBe(businessNumber);
    }

    [Fact]
    public void SupplierStatus_Create_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var status = "Active";
        var supplierProtected = "No";
        var standardIndustryClassification = "NAICS123";

        // Act
        var supplierStatus = new SupplierStatus(status, supplierProtected, standardIndustryClassification);

        // Assert
        supplierStatus.Status.ShouldBe(status);
        supplierStatus.SupplierProtected.ShouldBe(supplierProtected);
        supplierStatus.StandardIndustryClassification.ShouldBe(standardIndustryClassification);
    }

    [Fact]
    public void CasMetadata_Create_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var lastUpdated = DateTime.UtcNow;

        // Act
        var casMetadata = new CasMetadata(lastUpdated);

        // Assert
        casMetadata.LastUpdatedInCAS.ShouldBe(lastUpdated);
    }

    #endregion

    #region Value Object Equality Tests (Records)

    [Fact]
    public void SupplierBasicInfo_EqualRecords_ShouldBeEqual()
    {
        // Arrange
        var basicInfo1 = new SupplierBasicInfo("Test", "SUP001", "Category");
        var basicInfo2 = new SupplierBasicInfo("Test", "SUP001", "Category");

        // Assert
        basicInfo1.ShouldBe(basicInfo2);
        (basicInfo1 == basicInfo2).ShouldBeTrue();
        basicInfo1.GetHashCode().ShouldBe(basicInfo2.GetHashCode());
    }

    [Fact]
    public void ProviderInfo_DifferentRecords_ShouldNotBeEqual()
    {
        // Arrange
        var providerInfo1 = new ProviderInfo("PROV123", "BN111");
        var providerInfo2 = new ProviderInfo("PROV123", "BN222");

        // Assert
        providerInfo1.ShouldNotBe(providerInfo2);
        (providerInfo1 == providerInfo2).ShouldBeFalse();
    }

    #endregion

    #region Supplier Constructor with Value Objects Tests

    [Fact]
    public void Supplier_CreateWithValueObjects_ShouldSetAllPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var basicInfo = new SupplierBasicInfo("Test Supplier", "SUP001", "Category A");
        var providerInfo = new ProviderInfo("PROV123", "BN123456789");
        var supplierStatus = new SupplierStatus("Active", "No", "NAICS123");
        var casMetadata = new CasMetadata(DateTime.UtcNow);
        var correlation = new Correlation(Guid.NewGuid(), "CAS");
        var mailingAddress = new MailingAddress("123 Main St", "Victoria", "BC", "V8V1A1");

        // Act
        var supplier = new Supplier(id, basicInfo, correlation, providerInfo, supplierStatus, casMetadata, mailingAddress);

        // Assert
        supplier.Id.ShouldBe(id);
        supplier.Name.ShouldBe(basicInfo.Name);
        supplier.Number.ShouldBe(basicInfo.Number);
        supplier.Subcategory.ShouldBe(basicInfo.Subcategory);
        supplier.ProviderId.ShouldBe(providerInfo.ProviderId);
        supplier.BusinessNumber.ShouldBe(providerInfo.BusinessNumber);
        supplier.Status.ShouldBe(supplierStatus.Status);
        supplier.SupplierProtected.ShouldBe(supplierStatus.SupplierProtected);
        supplier.StandardIndustryClassification.ShouldBe(supplierStatus.StandardIndustryClassification);
        supplier.LastUpdatedInCAS.ShouldBe(casMetadata.LastUpdatedInCAS);
        supplier.CorrelationId.ShouldBe(correlation.CorrelationId);
        supplier.CorrelationProvider.ShouldBe(correlation.CorrelationProvider);
        supplier.MailingAddress.ShouldBe(mailingAddress.AddressLine);
        supplier.City.ShouldBe(mailingAddress.City);
        supplier.Province.ShouldBe(mailingAddress.Province);
        supplier.PostalCode.ShouldBe(mailingAddress.PostalCode);
        supplier.Sites.ShouldNotBeNull();
        supplier.Sites.Count.ShouldBe(0);
    }

    [Fact]
    public void Supplier_CreateWithNullOptionalValueObjects_ShouldHandleNullsCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var basicInfo = new SupplierBasicInfo("Test Supplier", "SUP001");
        var correlation = new Correlation(Guid.NewGuid(), "CAS");

        // Act
        var supplier = new Supplier(id, basicInfo, correlation);

        // Assert
        supplier.Id.ShouldBe(id);
        supplier.Name.ShouldBe("Test Supplier");
        supplier.Number.ShouldBe("SUP001");
        supplier.Subcategory.ShouldBeNull();
        supplier.ProviderId.ShouldBeNull();
        supplier.BusinessNumber.ShouldBeNull();
        supplier.Status.ShouldBeNull();
        supplier.SupplierProtected.ShouldBeNull();
        supplier.StandardIndustryClassification.ShouldBeNull();
        supplier.LastUpdatedInCAS.ShouldBeNull();
        supplier.MailingAddress.ShouldBeNull();
        supplier.City.ShouldBeNull();
        supplier.Province.ShouldBeNull();
        supplier.PostalCode.ShouldBeNull();
    }

    [Fact]
    public void Supplier_CreateWithPartialValueObjects_ShouldSetOnlyProvidedValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var basicInfo = new SupplierBasicInfo("Test Supplier", "SUP001", "Category");
        var providerInfo = new ProviderInfo("PROV123", null); // Only ProviderId, no BusinessNumber
        var correlation = new Correlation(Guid.NewGuid(), "CAS");

        // Act
        var supplier = new Supplier(id, basicInfo, correlation, providerInfo: providerInfo);

        // Assert
        supplier.Name.ShouldBe("Test Supplier");
        supplier.Number.ShouldBe("SUP001");
        supplier.Subcategory.ShouldBe("Category");
        supplier.ProviderId.ShouldBe("PROV123");
        supplier.BusinessNumber.ShouldBeNull();
        supplier.Status.ShouldBeNull(); // Not provided
        supplier.LastUpdatedInCAS.ShouldBeNull(); // Not provided
    }

    #endregion

    #region Update Methods Tests

    [Fact]
    public void Supplier_UpdateBasicInfo_ShouldUpdateCorrectProperties()
    {
        // Arrange
        var supplier = CreateTestSupplier();
        var newBasicInfo = new SupplierBasicInfo("Updated Name", "SUP999", "Updated Category");

        // Act
        supplier.UpdateBasicInfo(newBasicInfo);

        // Assert
        supplier.Name.ShouldBe("Updated Name");
        supplier.Number.ShouldBe("SUP999");
        supplier.Subcategory.ShouldBe("Updated Category");
        // Other properties should remain unchanged
        supplier.ProviderId.ShouldBe("PROV123");
        supplier.Status.ShouldBe("Active");
    }

    [Fact]
    public void Supplier_UpdateProviderInfo_ShouldUpdateCorrectProperties()
    {
        // Arrange
        var supplier = CreateTestSupplier();
        var newProviderInfo = new ProviderInfo("NEWPROV456", "NEWBN987654321");

        // Act
        supplier.UpdateProviderInfo(newProviderInfo);

        // Assert
        supplier.ProviderId.ShouldBe("NEWPROV456");
        supplier.BusinessNumber.ShouldBe("NEWBN987654321");
        // Other properties should remain unchanged
        supplier.Name.ShouldBe("Test Supplier");
        supplier.Status.ShouldBe("Active");
    }

    [Fact]
    public void Supplier_UpdateStatus_ShouldUpdateCorrectProperties()
    {
        // Arrange
        var supplier = CreateTestSupplier();
        var newStatus = new SupplierStatus("Inactive", "Yes", "NEWNAICS456");

        // Act
        supplier.UpdateStatus(newStatus);

        // Assert
        supplier.Status.ShouldBe("Inactive");
        supplier.SupplierProtected.ShouldBe("Yes");
        supplier.StandardIndustryClassification.ShouldBe("NEWNAICS456");
        // Other properties should remain unchanged
        supplier.Name.ShouldBe("Test Supplier");
        supplier.ProviderId.ShouldBe("PROV123");
    }

    [Fact]
    public void Supplier_UpdateCasMetadata_ShouldUpdateCorrectProperties()
    {
        // Arrange
        var supplier = CreateTestSupplier();
        var newDate = DateTime.UtcNow.AddDays(1);
        var newCasMetadata = new CasMetadata(newDate);

        // Act
        supplier.UpdateCasMetadata(newCasMetadata);

        // Assert
        supplier.LastUpdatedInCAS.ShouldBe(newDate);
        // Other properties should remain unchanged
        supplier.Name.ShouldBe("Test Supplier");
        supplier.Status.ShouldBe("Active");
    }

    #endregion

    #region Backward Compatibility Tests

    [Fact]
    public void Supplier_LegacyConstructor_ShouldStillWork()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Legacy Supplier";
        var number = "LEG001";
        var correlation = new Correlation(Guid.NewGuid(), "Legacy");
        var mailingAddress = new MailingAddress("Legacy Address", "Legacy City", "BC", "L3G4CY");

        // Act
        var supplier = new Supplier(id, name, number, correlation, mailingAddress);

        // Assert
        supplier.Id.ShouldBe(id);
        supplier.Name.ShouldBe(name);
        supplier.Number.ShouldBe(number);
        supplier.CorrelationId.ShouldBe(correlation.CorrelationId);
        supplier.CorrelationProvider.ShouldBe(correlation.CorrelationProvider);
        supplier.MailingAddress.ShouldBe(mailingAddress.AddressLine);
        supplier.City.ShouldBe(mailingAddress.City);
        supplier.Province.ShouldBe(mailingAddress.Province);
        supplier.PostalCode.ShouldBe(mailingAddress.PostalCode);
        
        // Properties not set in legacy constructor should have default values (string.Empty, not null)
        supplier.Subcategory.ShouldBe(string.Empty);
        supplier.ProviderId.ShouldBe(string.Empty);
        supplier.BusinessNumber.ShouldBe(string.Empty);
        supplier.Status.ShouldBe(string.Empty);
        supplier.SupplierProtected.ShouldBe(string.Empty);
        supplier.StandardIndustryClassification.ShouldBe(string.Empty);
        supplier.LastUpdatedInCAS.ShouldBeNull(); // This one is nullable DateTime, so remains null
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void ValueObjects_WithNullValues_ShouldHandleGracefully()
    {
        // Arrange & Act
        var basicInfo = new SupplierBasicInfo(null, null, null);
        var providerInfo = new ProviderInfo(null, null);
        var supplierStatus = new SupplierStatus(null, null, null);
        var casMetadata = new CasMetadata(null);

        // Assert
        basicInfo.Name.ShouldBeNull();
        basicInfo.Number.ShouldBeNull();
        basicInfo.Subcategory.ShouldBeNull();
        providerInfo.ProviderId.ShouldBeNull();
        providerInfo.BusinessNumber.ShouldBeNull();
        supplierStatus.Status.ShouldBeNull();
        supplierStatus.SupplierProtected.ShouldBeNull();
        supplierStatus.StandardIndustryClassification.ShouldBeNull();
        casMetadata.LastUpdatedInCAS.ShouldBeNull();
    }

    [Fact]
    public void ValueObjects_WithDefaultParameters_ShouldWork()
    {
        // Test that default parameters work correctly
        var basicInfo1 = new SupplierBasicInfo("Name", "Number");
        var basicInfo2 = new SupplierBasicInfo("Name", "Number", default);
        
        basicInfo1.ShouldBe(basicInfo2);
        basicInfo1.Subcategory.ShouldBeNull();
        
        var providerInfo1 = new ProviderInfo("Provider");
        var providerInfo2 = new ProviderInfo("Provider", default);
        
        providerInfo1.ShouldBe(providerInfo2);
        providerInfo1.BusinessNumber.ShouldBeNull();
    }

    #endregion

    #region Helper Methods

    private Supplier CreateTestSupplier()
    {
        var id = Guid.NewGuid();
        var basicInfo = new SupplierBasicInfo("Test Supplier", "SUP001", "Category A");
        var providerInfo = new ProviderInfo("PROV123", "BN123456789");
        var supplierStatus = new SupplierStatus("Active", "No", "NAICS123");
        var casMetadata = new CasMetadata(DateTime.UtcNow);
        var correlation = new Correlation(Guid.NewGuid(), "Test");
        var mailingAddress = new MailingAddress("123 Test St", "Test City", "BC", "T3ST1NG");

        return new Supplier(id, basicInfo, correlation, providerInfo, supplierStatus, casMetadata, mailingAddress);
    }

    #endregion
}
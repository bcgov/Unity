using System;
using Shouldly;
using Unity.Modules.Shared.Correlation;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Domain.Suppliers.ValueObjects;
using Xunit;
using System.ComponentModel;

namespace Unity.Payments.Suppliers;

/// <summary>
/// Integration tests that verify the value object refactoring maintains backward compatibility
/// while providing the benefits of better organization and type safety.
/// </summary>
[Category("Integration")]
public class Supplier_ValueObject_Refactoring_Integration_Tests
{
    #region Refactoring Benefits Demonstration

    [Fact]
    public void ValueObjectRefactoring_ShouldImproveParameterOrganization()
    {
        // Before: Constructor with 12+ individual parameters was hard to maintain
        // After: Constructor with 6 logical value object groups

        // Arrange - Create test data that would have been 12+ individual parameters
        var id = Guid.NewGuid();
        var name = "Refactoring Demo Supplier";
        var number = "RDS001";
        var subcategory = "Company";
        var providerId = "PROV456";
        var businessNumber = "BN987654321";
        var status = "Active";
        var supplierProtected = "No";
        var standardIndustryClassification = "NAICS789";
        var lastUpdatedInCAS = DateTime.UtcNow;
        var correlationId = Guid.NewGuid();
        var correlationProvider = "DemoProvider";
        var mailingAddress = "123 Demo Street";
        var city = "Demo City";
        var province = "BC";
        var postalCode = "DEMO123";

        // Act - Create supplier using new value object approach
        var basicInfo = new SupplierBasicInfo(name, number, subcategory);
        var providerInfo = new ProviderInfo(providerId, businessNumber);
        var supplierStatus = new SupplierStatus(status, supplierProtected, standardIndustryClassification);
        var casMetadata = new CasMetadata(lastUpdatedInCAS);
        var correlation = new Correlation(correlationId, correlationProvider);
        var mailingAddressVO = new MailingAddress(mailingAddress, city, province, postalCode);

        var supplier = new Supplier(id, basicInfo, correlation, providerInfo, supplierStatus, casMetadata, mailingAddressVO);

        // Assert - Verify all data is correctly set
        supplier.Id.ShouldBe(id);
        supplier.Name.ShouldBe(name);
        supplier.Number.ShouldBe(number);
        supplier.Subcategory.ShouldBe(subcategory);
        supplier.ProviderId.ShouldBe(providerId);
        supplier.BusinessNumber.ShouldBe(businessNumber);
        supplier.Status.ShouldBe(status);
        supplier.SupplierProtected.ShouldBe(supplierProtected);
        supplier.StandardIndustryClassification.ShouldBe(standardIndustryClassification);
        supplier.LastUpdatedInCAS.ShouldBe(lastUpdatedInCAS);
        supplier.CorrelationId.ShouldBe(correlationId);
        supplier.CorrelationProvider.ShouldBe(correlationProvider);
        supplier.MailingAddress.ShouldBe(mailingAddress);
        supplier.City.ShouldBe(city);
        supplier.Province.ShouldBe(province);
        supplier.PostalCode.ShouldBe(postalCode);

        // Benefits achieved:
        // 1. Related parameters grouped into logical units
        // 2. Type safety - can't accidentally swap similar parameters
        // 3. Easier to extend - add fields to value objects without changing method signatures
        // 4. Better maintainability - clear separation of concerns
        // 5. Immutable value objects provide additional safety
    }

    [Fact]
    public void ValueObjectRefactoring_ShouldMaintainBackwardCompatibility()
    {
        // This test ensures the refactoring doesn't break existing functionality

        // Arrange - Use both old and new approaches with same data
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var testData = new
        {
            Name = "Compatibility Test",
            Number = "COMPAT001",
            Subcategory = "Individual",
            ProviderId = "COMP_PROV",
            BusinessNumber = "COMP_BN123",
            Status = "Active",
            SupplierProtected = "No",
            StandardIndustryClassification = "COMP_NAICS",
            LastUpdatedInCAS = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid(),
            CorrelationProvider = "CompatTest",
            MailingAddress = "456 Compat Ave",
            City = "Compat City",
            Province = "AB",
            PostalCode = "COMP456"
        };

        // Act - Create using legacy constructor
        var legacySupplier = new Supplier(
            id1,
            testData.Name,
            testData.Number,
            new Correlation(testData.CorrelationId, testData.CorrelationProvider),
            new MailingAddress(testData.MailingAddress, testData.City, testData.Province, testData.PostalCode));
        
        // Manually set properties that weren't in legacy constructor
        legacySupplier.Subcategory = testData.Subcategory;
        legacySupplier.ProviderId = testData.ProviderId;
        legacySupplier.BusinessNumber = testData.BusinessNumber;
        legacySupplier.Status = testData.Status;
        legacySupplier.SupplierProtected = testData.SupplierProtected;
        legacySupplier.StandardIndustryClassification = testData.StandardIndustryClassification;
        legacySupplier.LastUpdatedInCAS = testData.LastUpdatedInCAS;

        // Create using new value object constructor
        var newSupplier = new Supplier(
            id2,
            new SupplierBasicInfo(testData.Name, testData.Number, testData.Subcategory),
            new Correlation(testData.CorrelationId, testData.CorrelationProvider),
            new ProviderInfo(testData.ProviderId, testData.BusinessNumber),
            new SupplierStatus(testData.Status, testData.SupplierProtected, testData.StandardIndustryClassification),
            new CasMetadata(testData.LastUpdatedInCAS),
            new MailingAddress(testData.MailingAddress, testData.City, testData.Province, testData.PostalCode));

        // Assert - Both approaches should produce identical domain state
        legacySupplier.Name.ShouldBe(newSupplier.Name);
        legacySupplier.Number.ShouldBe(newSupplier.Number);
        legacySupplier.Subcategory.ShouldBe(newSupplier.Subcategory);
        legacySupplier.ProviderId.ShouldBe(newSupplier.ProviderId);
        legacySupplier.BusinessNumber.ShouldBe(newSupplier.BusinessNumber);
        legacySupplier.Status.ShouldBe(newSupplier.Status);
        legacySupplier.SupplierProtected.ShouldBe(newSupplier.SupplierProtected);
        legacySupplier.StandardIndustryClassification.ShouldBe(newSupplier.StandardIndustryClassification);
        legacySupplier.LastUpdatedInCAS.ShouldBe(newSupplier.LastUpdatedInCAS);
        legacySupplier.CorrelationId.ShouldBe(newSupplier.CorrelationId);
        legacySupplier.CorrelationProvider.ShouldBe(newSupplier.CorrelationProvider);
        legacySupplier.MailingAddress.ShouldBe(newSupplier.MailingAddress);
        legacySupplier.City.ShouldBe(newSupplier.City);
        legacySupplier.Province.ShouldBe(newSupplier.Province);
        legacySupplier.PostalCode.ShouldBe(newSupplier.PostalCode);

        // Backward compatibility maintained ?
    }

    [Fact]
    public void ValueObjectRefactoring_ShouldProvideTypeSafety()
    {
        // This test demonstrates improved type safety with value objects

        // Arrange
        var basicInfo = new SupplierBasicInfo("Supplier Name", "SUP001", "Category");
        var providerInfo = new ProviderInfo("PROV123", "BN456789");
        var supplierStatus = new SupplierStatus("Active", "No", "NAICS123");

        // Act & Assert - Value objects prevent parameter confusion
        // Before: Easy to accidentally swap parameters of same type (string)
        // new Supplier(id, "PROV123", "SUP001", "Supplier Name", ...) // Accidentally swapped!
        
        // After: Impossible to swap because each value object has distinct type
        var supplier = new Supplier(
            Guid.NewGuid(),
            basicInfo,     // Can't pass providerInfo here - compiler error
            new Correlation(Guid.NewGuid(), "Provider"),
            providerInfo,  // Can't pass basicInfo here - compiler error
            supplierStatus,
            new CasMetadata(DateTime.UtcNow),
            new MailingAddress("Address", "City", "Province", "PostalCode"));

        // Value objects provide compile-time safety ?
        supplier.ShouldNotBeNull();
        basicInfo.Name.ShouldBe("Supplier Name");
        providerInfo.ProviderId.ShouldBe("PROV123");
        supplierStatus.Status.ShouldBe("Active");
    }

    [Fact]
    public void ValueObjectRefactoring_ShouldSupportUpdateOperations()
    {
        // This test verifies that the new update methods work correctly with value objects

        // Arrange
        var supplier = new Supplier(
            Guid.NewGuid(),
            new SupplierBasicInfo("Original Name", "ORIG001", "Original Category"),
            new Correlation(Guid.NewGuid(), "Original"));

        // Act - Update using value object methods
        supplier.UpdateBasicInfo(new SupplierBasicInfo("Updated Name", "UPD001", "Updated Category"));
        supplier.UpdateProviderInfo(new ProviderInfo("NEW_PROV", "NEW_BN"));
        supplier.UpdateStatus(new SupplierStatus("Inactive", "Yes", "NEW_NAICS"));
        supplier.UpdateCasMetadata(new CasMetadata(DateTime.UtcNow));

        // Assert - Verify updates were applied correctly
        supplier.Name.ShouldBe("Updated Name");
        supplier.Number.ShouldBe("UPD001");
        supplier.Subcategory.ShouldBe("Updated Category");
        supplier.ProviderId.ShouldBe("NEW_PROV");
        supplier.BusinessNumber.ShouldBe("NEW_BN");
        supplier.Status.ShouldBe("Inactive");
        supplier.SupplierProtected.ShouldBe("Yes");
        supplier.StandardIndustryClassification.ShouldBe("NEW_NAICS");
        supplier.LastUpdatedInCAS.ShouldNotBeNull();

        // Update methods provide clean, grouped operations ?
    }

    [Fact]
    public void ValueObjectRefactoring_ShouldHandlePartialData()
    {
        // This test verifies that optional value objects work correctly

        // Arrange & Act - Create supplier with only required data
        var supplier = new Supplier(
            Guid.NewGuid(),
            new SupplierBasicInfo("Minimal Supplier", "MIN001"),
            new Correlation(Guid.NewGuid(), "MinimalProvider"));
        // Optional parameters: providerInfo, supplierStatus, casMetadata, mailingAddress all default to null

        // Assert - Required data set, optional data has appropriate defaults
        supplier.Name.ShouldBe("Minimal Supplier");
        supplier.Number.ShouldBe("MIN001");
        supplier.Subcategory.ShouldBeNull(); // Optional in SupplierBasicInfo
        
        // Properties from optional value objects should be null when value objects not provided
        // The null-conditional operator returns null when the value object is null
        supplier.ProviderId.ShouldBeNull(); // providerInfo?.ProviderId returns null
        supplier.BusinessNumber.ShouldBeNull(); // providerInfo?.BusinessNumber returns null
        supplier.Status.ShouldBeNull(); // supplierStatus?.Status returns null
        supplier.SupplierProtected.ShouldBeNull(); // supplierStatus?.SupplierProtected returns null
        supplier.StandardIndustryClassification.ShouldBeNull(); // supplierStatus?.StandardIndustryClassification returns null
        supplier.LastUpdatedInCAS.ShouldBeNull(); // casMetadata?.LastUpdatedInCAS returns null
        supplier.MailingAddress.ShouldBeNull(); // mailingAddress?.AddressLine returns null
        supplier.City.ShouldBeNull();
        supplier.Province.ShouldBeNull();
        supplier.PostalCode.ShouldBeNull();

        // Partial data creation works correctly ?
    }

    #endregion

    #region Value Object Record Benefits

    [Fact]
    public void ValueObjectRecords_ShouldProvideValueEquality()
    {
        // This test demonstrates the benefits of using records for value objects

        // Arrange & Act
        var basicInfo1 = new SupplierBasicInfo("Test Supplier", "TEST001", "Category");
        var basicInfo2 = new SupplierBasicInfo("Test Supplier", "TEST001", "Category");
        var basicInfo3 = new SupplierBasicInfo("Different Supplier", "DIFF001", "Category");

        // Assert - Records provide automatic value equality
        basicInfo1.ShouldBe(basicInfo2); // Same values = equal
        basicInfo1.ShouldNotBe(basicInfo3); // Different values = not equal
        (basicInfo1 == basicInfo2).ShouldBeTrue();
        (basicInfo1 == basicInfo3).ShouldBeFalse();
        basicInfo1.GetHashCode().ShouldBe(basicInfo2.GetHashCode()); // Same hash for equal values

        // Records provide immutability and value equality out of the box ?
    }

    [Fact]
    public void ValueObjectRecords_ShouldSupportWithExpressions()
    {
        // This test demonstrates record 'with' expressions for modifications

        // Arrange
        var originalBasicInfo = new SupplierBasicInfo("Original Name", "ORIG001", "Original Category");

        // Act - Use 'with' expressions to create modified copies
        var modifiedName = originalBasicInfo with { Name = "Modified Name" };
        var modifiedNumber = originalBasicInfo with { Number = "MOD001" };
        var modifiedCategory = originalBasicInfo with { Subcategory = "Modified Category" };

        // Assert - Original unchanged, new instances created with modifications
        originalBasicInfo.Name.ShouldBe("Original Name");
        originalBasicInfo.Number.ShouldBe("ORIG001");
        originalBasicInfo.Subcategory.ShouldBe("Original Category");

        modifiedName.Name.ShouldBe("Modified Name");
        modifiedName.Number.ShouldBe("ORIG001"); // Unchanged
        modifiedName.Subcategory.ShouldBe("Original Category"); // Unchanged

        modifiedNumber.Name.ShouldBe("Original Name"); // Unchanged
        modifiedNumber.Number.ShouldBe("MOD001");
        modifiedNumber.Subcategory.ShouldBe("Original Category"); // Unchanged

        // Records provide convenient immutable modification patterns ?
    }

    #endregion

    #region Integration with CAS Service Scenario

    [Fact]
    public void ValueObjectRefactoring_ShouldWorkWithCasIntegration()
    {
        // This test simulates how the value objects work in a CAS integration scenario

        // Arrange - Simulate CAS response data
        var casSupplierData = new
        {
            suppliername = "CAS Test Supplier",
            suppliernumber = "CAS001",
            subcategory = "Individual",
            providerid = "CAS_PROV_123",
            businessnumber = "CAS_BN_987654321",
            status = "ACTIVE",
            supplierprotected = "N",
            standardindustryclassification = "CAS_NAICS_456",
            lastupdated = DateTime.UtcNow.AddDays(-1),
            correlationid = Guid.NewGuid(),
            correlationprovider = "CAS",
            mailingaddress = "789 CAS Boulevard",
            city = "CAS City",
            province = "BC",
            postalcode = "CAS123"
        };

        // Act - Transform CAS data into value objects (as SupplierService would do)
        var basicInfo = new SupplierBasicInfo(
            casSupplierData.suppliername,
            casSupplierData.suppliernumber,
            casSupplierData.subcategory);

        var providerInfo = new ProviderInfo(
            casSupplierData.providerid,
            casSupplierData.businessnumber);

        var supplierStatus = new SupplierStatus(
            casSupplierData.status,
            casSupplierData.supplierprotected,
            casSupplierData.standardindustryclassification);

        var casMetadata = new CasMetadata(casSupplierData.lastupdated);

        var correlation = new Correlation(
            casSupplierData.correlationid,
            casSupplierData.correlationprovider);

        var mailingAddress = new MailingAddress(
            casSupplierData.mailingaddress,
            casSupplierData.city,
            casSupplierData.province,
            casSupplierData.postalcode);

        // Create supplier from CAS data using value objects
        var supplier = new Supplier(
            Guid.NewGuid(),
            basicInfo,
            correlation,
            providerInfo,
            supplierStatus,
            casMetadata,
            mailingAddress);

        // Assert - Verify CAS data correctly mapped to domain object
        supplier.Name.ShouldBe(casSupplierData.suppliername);
        supplier.Number.ShouldBe(casSupplierData.suppliernumber);
        supplier.Subcategory.ShouldBe(casSupplierData.subcategory);
        supplier.ProviderId.ShouldBe(casSupplierData.providerid);
        supplier.BusinessNumber.ShouldBe(casSupplierData.businessnumber);
        supplier.Status.ShouldBe(casSupplierData.status);
        supplier.SupplierProtected.ShouldBe(casSupplierData.supplierprotected);
        supplier.StandardIndustryClassification.ShouldBe(casSupplierData.standardindustryclassification);
        supplier.LastUpdatedInCAS.ShouldBe(casSupplierData.lastupdated);
        supplier.CorrelationId.ShouldBe(casSupplierData.correlationid);
        supplier.CorrelationProvider.ShouldBe(casSupplierData.correlationprovider);

        // Value objects work seamlessly with external integrations ?
    }

    #endregion
}
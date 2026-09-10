using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using Unity.GrantManager.Web.Views.Shared.Components.ApplicantAddresses;
using Xunit;

namespace Unity.GrantManager.Components;

public class ApplicantAddressesViewModelTests
{
    [Theory]
    [InlineData(nameof(ApplicantPrimaryAddressViewModel.Street))]
    [InlineData(nameof(ApplicantPrimaryAddressViewModel.Street2))]
    [InlineData(nameof(ApplicantPrimaryAddressViewModel.Unit))]
    [InlineData(nameof(ApplicantPrimaryAddressViewModel.City))]
    [InlineData(nameof(ApplicantPrimaryAddressViewModel.Province))]
    [InlineData(nameof(ApplicantPrimaryAddressViewModel.PostalCode))]
    public void Should_NotRequireIndividualAddressFields_InMvcValidation(string propertyName)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvcCore().AddDataAnnotations();
        using var provider = services.BuildServiceProvider();
        var metadataProvider = provider.GetRequiredService<IModelMetadataProvider>();

        var property = metadataProvider.GetMetadataForType(typeof(ApplicantPrimaryAddressViewModel))
            .Properties[propertyName];

        property.ShouldNotBeNull();
        property.IsRequired.ShouldBeFalse();
    }

    [Fact]
    public void EditorWithAnApplicationCanFillBothMissingAddresses()
    {
        var model = new ApplicantAddressesViewModel
        {
            CanEditAddresses = true,
            ExpectedApplicationId = Guid.NewGuid()
        };

        model.CanEditPrimaryPhysical.ShouldBeTrue();
        model.CanEditPrimaryMailing.ShouldBeTrue();
        model.CanSave.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingOrEmptyApplicationPreventsCreation(bool emptyId)
    {
        var model = new ApplicantAddressesViewModel
        {
            CanEditAddresses = true,
            ExpectedApplicationId = emptyId ? Guid.Empty : null
        };

        model.CanCreateAddresses.ShouldBeFalse();
        model.CanEditPrimaryPhysical.ShouldBeFalse();
        model.CanEditPrimaryMailing.ShouldBeFalse();
        model.CanSave.ShouldBeFalse();
    }

    [Fact]
    public void ExistingAddressRemainsEditableWithoutApplications()
    {
        var model = new ApplicantAddressesViewModel
        {
            CanEditAddresses = true,
            PrimaryPhysicalAddress = new() { Id = Guid.NewGuid() }
        };

        model.CanEditPrimaryPhysical.ShouldBeTrue();
        model.CanEditPrimaryMailing.ShouldBeFalse();
        model.CanSave.ShouldBeTrue();
    }

    [Fact]
    public void ViewerCannotEditOrCreateEvenWithAnApplicationAndExistingAddress()
    {
        var model = new ApplicantAddressesViewModel
        {
            CanEditAddresses = false,
            ExpectedApplicationId = Guid.NewGuid(),
            PrimaryPhysicalAddress = new() { Id = Guid.NewGuid() }
        };

        model.CanEditPrimaryPhysical.ShouldBeFalse();
        model.CanEditPrimaryMailing.ShouldBeFalse();
        model.CanSave.ShouldBeFalse();
    }
}

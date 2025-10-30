using Microsoft.EntityFrameworkCore;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.EntityFrameworkCore;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Intakes;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace Unity.GrantManager.Repositories;

public class ApplicationRepositoryTests_FilteringIssue : GrantManagerEntityFrameworkCoreTestBase
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IRepository<Application, Guid> _applicationIRepository;
    private readonly IRepository<Applicant, Guid> _applicantRepository;
    private readonly IRepository<ApplicantAddress, Guid> _applicantAddressRepository;
    private readonly IRepository<ApplicationForm, Guid> _applicationFormRepository;
    private readonly IRepository<ApplicationStatus, Guid> _applicationStatusRepository;
    private readonly IRepository<Intake, Guid> _intakeRepository;

    public ApplicationRepositoryTests_FilteringIssue()
    {
        _applicationRepository = GetRequiredService<IApplicationRepository>();
        _applicationIRepository = GetRequiredService<IRepository<Application, Guid>>();
        _applicantRepository = GetRequiredService<IRepository<Applicant, Guid>>();
        _applicantAddressRepository = GetRequiredService<IRepository<ApplicantAddress, Guid>>();
        _applicationFormRepository = GetRequiredService<IRepository<ApplicationForm, Guid>>();
        _applicationStatusRepository = GetRequiredService<IRepository<ApplicationStatus, Guid>>();
        _intakeRepository = GetRequiredService<IRepository<Intake, Guid>>();        
    }

    [Fact]
    public async Task WithBasicDetailsAsync_Should_Filter_ApplicantAddresses_By_ApplicationId()
    {
        // Arrange
        Guid applicantId = Guid.Empty;
        Guid application1Id = Guid.Empty;
        Guid application2Id = Guid.Empty;
        Guid intakeId = Guid.Empty;
        Guid applicationFormId = Guid.Empty;
        Guid applicationStatusId = Guid.Empty;

        await WithUnitOfWorkAsync(async () =>
        {
            // Create required entities first
            // Create intake
            var intake = new Intake
            {
                IntakeName = "Test Intake",
                StartDate = DateTime.Now.AddDays(-30),
                EndDate = DateTime.Now.AddDays(30)
            };
            await _intakeRepository.InsertAsync(intake, autoSave: true);
            intakeId = intake.Id;

            // Create application form
            var applicationForm = new ApplicationForm
            {
                IntakeId = intakeId,
                ApplicationFormName = "Test Form",
                ChefsApplicationFormGuid = Guid.NewGuid().ToString()
            };
            await _applicationFormRepository.InsertAsync(applicationForm, autoSave: true);
            applicationFormId = applicationForm.Id;

            // Create application status (check if exists first)
            var applicationStatus = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == GrantApplicationState.SUBMITTED);
            if (applicationStatus == null)
            {
                applicationStatus = new ApplicationStatus
                {
                    StatusCode = GrantApplicationState.SUBMITTED,
                    ExternalStatus = "Submitted",
                    InternalStatus = "Submitted"
                };
                await _applicationStatusRepository.InsertAsync(applicationStatus, autoSave: true);
            }
            applicationStatusId = applicationStatus.Id;

            // Create an applicant
            var applicant = new Applicant
            {
                ApplicantName = "Test Applicant",
                OrgName = "Test Org"
            };
            await _applicantRepository.InsertAsync(applicant, autoSave: true);
            applicantId = applicant.Id;

            // Create two applications with the same applicant
            var application1 = new Application
            {
                ApplicantId = applicantId,
                ApplicationFormId = applicationFormId,
                ApplicationStatusId = applicationStatusId,
                ProjectName = "Application 1",
                ReferenceNo = "REF001"
            };
            await _applicationIRepository.InsertAsync(application1, autoSave: true);
            application1Id = application1.Id;

            var application2 = new Application
            {
                ApplicantId = applicantId,
                ApplicationFormId = applicationFormId,
                ApplicationStatusId = applicationStatusId,
                ProjectName = "Application 2",
                ReferenceNo = "REF002"
            };
            await _applicationIRepository.InsertAsync(application2, autoSave: true);
            application2Id = application2.Id;

            // Create addresses for both applications
            var address1 = new ApplicantAddress
            {
                ApplicantId = applicantId,
                ApplicationId = application1Id,
                Street = "123 Main St",
                City = "City1"
            };
            await _applicantAddressRepository.InsertAsync(address1, autoSave: true);

            var address2 = new ApplicantAddress
            {
                ApplicantId = applicantId,
                ApplicationId = application2Id,
                Street = "456 Oak Ave",
                City = "City2"
            };
            await _applicantAddressRepository.InsertAsync(address2, autoSave: true);

            var address3 = new ApplicantAddress
            {
                ApplicantId = applicantId,
                ApplicationId = application2Id,
                Street = "789 Pine Rd",
                City = "City3"
            };
            await _applicantAddressRepository.InsertAsync(address3, autoSave: true);
        });

        // Act - Test the repository method
        Application? result = null;
        await WithUnitOfWorkAsync(async () =>
        {
            result = await _applicationRepository.WithBasicDetailsAsync(application1Id);
        });

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(application1Id);
        result.Applicant.ShouldNotBeNull();
        result.Applicant.ApplicantAddresses.ShouldNotBeNull();       

        // This is what SHOULD happen if filtering works correctly
        result.Applicant.ApplicantAddresses.Count.ShouldBe(1, "Should only load addresses for application1");               
        result.Applicant.ApplicantAddresses.All(a => a.ApplicationId == application1Id).ShouldBeTrue("All addresses should belong to application1");
    }    
}
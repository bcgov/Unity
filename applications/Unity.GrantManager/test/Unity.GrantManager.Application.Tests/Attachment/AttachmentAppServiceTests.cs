using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.Comments;
using Unity.GrantManager.Exceptions;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Uow;
using Volo.Abp.Users;
using Xunit;

namespace Unity.GrantManager.GrantApplications;

public class AttachmentAppServiceTests : GrantManagerApplicationTestBase
{
    private readonly AttachmentService _attachmentAppServiceTest;
    private readonly IRepository<Application, Guid> _applicationsRepository;
    private readonly IRepository<Assessment, Guid> _assessmentsRepository;

    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public AttachmentAppServiceTests()
    {
        _attachmentAppServiceTest = GetRequiredService<AttachmentService>();
        _applicationsRepository = GetRequiredService<IRepository<Application, Guid>>();   
        _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
        _assessmentsRepository = GetRequiredService<IRepository<Assessment, Guid>>();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApplicationAsync_Should_List_Application_Attachments()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationsRepository.GetListAsync())[0];

        // Act
        var applicationAttachments = (await _attachmentAppServiceTest.GetApplicationAsync(application.Id)).ToList();

        // Assert            
        applicationAttachments.ShouldNotBeNull();
        applicationAttachments.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAssessmentAsync_Should_List_Assessment_Attachments()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationsRepository.GetListAsync())[0];
        var assessment = (await _assessmentsRepository.FirstOrDefaultAsync(s => s.ApplicationId.Equals(application.Id)));

        // Act
        var assessmentAttachments = (await _attachmentAppServiceTest.GetAssessmentAsync(assessment.Id)).ToList();

        // Assert            
        assessmentAttachments.ShouldNotBeNull();
        assessmentAttachments.Count.ShouldBeGreaterThan(0);
    }
}

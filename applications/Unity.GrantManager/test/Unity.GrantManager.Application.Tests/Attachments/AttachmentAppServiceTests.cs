using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Assessments;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.Attachments;

public class AttachmentAppServiceTests : GrantManagerApplicationTestBase
{
    private readonly AttachmentAppService _attachmentAppServiceTest;
    private readonly IRepository<Application, Guid> _applicationsRepository;
    private readonly IRepository<Assessment, Guid> _assessmentsRepository;

    private readonly IRepository<ApplicationAttachment, Guid> _applicationAttachmentRepository;
    private readonly IRepository<AssessmentAttachment, Guid> _assessmentAttachmentRepository;
    private readonly IRepository<ApplicationChefsFileAttachment, Guid> _chefsAttachmentRepository;

    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public AttachmentAppServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        _attachmentAppServiceTest = GetRequiredService<AttachmentAppService>();
        _applicationsRepository = GetRequiredService<IRepository<Application, Guid>>();
        _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
        _assessmentsRepository = GetRequiredService<IRepository<Assessment, Guid>>();

        _applicationAttachmentRepository = GetRequiredService<IRepository<ApplicationAttachment, Guid>>();
        _assessmentAttachmentRepository = GetRequiredService<IRepository<AssessmentAttachment, Guid>>();
        _chefsAttachmentRepository = GetRequiredService<IRepository<ApplicationChefsFileAttachment, Guid>>();
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
        var assessment = await _assessmentsRepository.FindAsync(s => s.ApplicationId.Equals(application.Id));

        // Act
        var assessmentAttachments = (await _attachmentAppServiceTest.GetAssessmentAsync(assessment!.Id)).ToList();

        // Assert            
        assessmentAttachments.ShouldNotBeNull();
        assessmentAttachments.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task GetApplicationChefsFileAttachmentsAsync_Should_Return_Attachments()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationsRepository.GetListAsync())[0];

        // Act
        var chefsAttachments = await _attachmentAppServiceTest.GetApplicationChefsFileAttachmentsAsync(application.Id);

        // Assert
        chefsAttachments.ShouldNotBeNull();
        // Optionally check count (>= 0)
        chefsAttachments.Count.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ResyncSubmissionAttachmentsAsync_Should_Not_Throw()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationsRepository.GetListAsync())[0];

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => _attachmentAppServiceTest.ResyncSubmissionAttachmentsAsync(application.Id));
        exception.ShouldBeNull();
    }

    #region GetAttachmentsAsync
    [Fact]
    public async Task GetAttachmentsAsync_For_Application_Should_Return_Application_Attachments()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationsRepository.GetListAsync())[0];

        // Act
        var attachments = await _attachmentAppServiceTest.GetAttachmentsAsync(new AttachmentParametersDto(AttachmentType.APPLICATION, application.Id));

        // Assert
        attachments.ShouldNotBeNull();
        attachments.ShouldAllBe(attachment => attachment.AttachmentType == AttachmentType.APPLICATION);
        attachments.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task GetAttachmentsAsync_For_Assessment_Should_Return_Assessment_Attachments()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationsRepository.GetListAsync())[0];
        var assessment = await _assessmentsRepository.FindAsync(s => s.ApplicationId.Equals(application.Id));

        // Act
        var attachments = await _attachmentAppServiceTest.GetAttachmentsAsync(new AttachmentParametersDto(AttachmentType.ASSESSMENT, assessment!.Id));

        // Assert
        attachments.ShouldNotBeNull();
        attachments.ShouldAllBe(attachment => attachment.AttachmentType == AttachmentType.ASSESSMENT);
        attachments.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task GetAttachmentsInternalAsync_Should_Return_Application_Attachments()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationsRepository.GetListAsync())[0];

        // Act
        var attachments = await _attachmentAppServiceTest.GetAttachmentsInternalAsync(
            _applicationAttachmentRepository,
            attachment => attachment.ApplicationId == application.Id);

        // Assert
        attachments.ShouldNotBeNull();
        attachments.ShouldAllBe(attachment => attachment.AttachmentType == AttachmentType.APPLICATION);
        attachments.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task GetAttachmentsInternalAsync_For_Assessment_Should_Return_Assessment_Attachments()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationsRepository.GetListAsync())[0];
        var assessment = await _assessmentsRepository.FindAsync(s => s.ApplicationId.Equals(application.Id));

        // Act
        var attachments = await _attachmentAppServiceTest.GetAttachmentsInternalAsync(
            _assessmentAttachmentRepository,
            attachment => attachment.AssessmentId == assessment!.Id);

        // Assert
        attachments.ShouldNotBeNull();
        attachments.ShouldAllBe(attachment => attachment.AttachmentType == AttachmentType.ASSESSMENT);
        attachments.Count.ShouldBeGreaterThan(0);
    }
    #endregion GetAttachmentsAsync

    #region GetAttachmentMetadataAsync
    [Fact]
    public async Task GetAttachmentMetadataAsync_For_Application_Should_Return_Metadata()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationsRepository.GetListAsync())[0];
        var attachment = (await _attachmentAppServiceTest.GetApplicationAsync(application.Id))[0];

        // Act
        var metadata = await _attachmentAppServiceTest.GetAttachmentMetadataAsync(AttachmentType.APPLICATION, attachment.Id);

        // Assert
        metadata.ShouldNotBeNull();
        metadata.Id.ShouldBe(attachment.Id);
        metadata.FileName.ShouldBe(attachment.FileName);
    }

    [Fact]
    public async Task GetAttachmentMetadataAsync_For_Assessment_Should_Return_Metadata()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationsRepository.GetListAsync())[0];
        var assessment = await _assessmentsRepository.FindAsync(s => s.ApplicationId.Equals(application.Id));
        var attachment = (await _attachmentAppServiceTest.GetAttachmentsAsync(new AttachmentParametersDto(AttachmentType.ASSESSMENT, assessment!.Id)))[0];

        // Act
        var metadata = await _attachmentAppServiceTest.GetAttachmentMetadataAsync(AttachmentType.ASSESSMENT, attachment.Id);

        // Assert
        metadata.ShouldNotBeNull();
        metadata.Id.ShouldBe(attachment.Id);
        metadata.FileName.ShouldBe(attachment.FileName);
    }

    [Fact]
    public async Task GetAttachmentMetadataAsync_For_Chefs_Should_Return_Metadata()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationsRepository.GetListAsync())[0];
        var attachment = (await _attachmentAppServiceTest.GetApplicationChefsFileAttachmentsAsync(application.Id))[0];

        // Act
        var metadata = await _attachmentAppServiceTest.GetAttachmentMetadataAsync(AttachmentType.CHEFS, attachment.Id);

        // Assert
        metadata.ShouldNotBeNull();
        metadata.Id.ShouldBe(attachment.Id);
        metadata.FileName.ShouldBe(attachment.FileName);
    }

    [Fact]
    public async Task GetMetadataInternalAsync_For_Application_Should_Return_Metadata()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationsRepository.GetListAsync())[0];
        var attachment = (await _attachmentAppServiceTest.GetApplicationAsync(application.Id))[0];

        // Act
        var metadata = await AttachmentAppService.GetMetadataInternalAsync(attachment.Id, _applicationAttachmentRepository);

        // Assert
        metadata.ShouldNotBeNull();
        metadata.Id.ShouldBe(attachment.Id);
        metadata.FileName.ShouldBe(attachment.FileName);
    }

    [Fact]
    public async Task GetMetadataInternalAsync_For_Assessment_Should_Return_Metadata()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationsRepository.GetListAsync())[0];
        var assessment = await _assessmentsRepository.FindAsync(s => s.ApplicationId.Equals(application.Id));
        var attachment = (await _attachmentAppServiceTest.GetAssessmentAsync(assessment!.Id))[0];

        // Act
        var metadata = await AttachmentAppService.GetMetadataInternalAsync(attachment.Id, _assessmentAttachmentRepository);

        // Assert
        metadata.ShouldNotBeNull();
        metadata.Id.ShouldBe(attachment.Id);
        metadata.FileName.ShouldBe(attachment.FileName);
    }

    [Fact]
    public async Task GetMetadataInternalAsync_For_Chefs_Should_Return_Metadata()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationsRepository.GetListAsync())[0];
        var attachment = (await _attachmentAppServiceTest.GetApplicationChefsFileAttachmentsAsync(application.Id))[0];

        // Act
        var metadata = await AttachmentAppService.GetMetadataInternalAsync(attachment.Id, _chefsAttachmentRepository);

        // Assert
        metadata.ShouldNotBeNull();
        metadata.Id.ShouldBe(attachment.Id);
        metadata.FileName.ShouldBe(attachment.FileName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAttachmentMetadataAsync_InvalidType_ThrowsArgumentException()
    {
        // Act & Assert: Using an invalid AttachmentType value.
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _attachmentAppServiceTest.GetAttachmentMetadataAsync((AttachmentType)999, Guid.NewGuid());
        });
    }
    #endregion GetAttachmentMetadataAsync

    #region UpdateAttachmentMetadataAsync
    [Fact]
    public async Task UpdateAttachmentMetadataAsync_For_Application_Should_Update_Metadata()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationsRepository.GetListAsync())[0];
        var attachment = (await _attachmentAppServiceTest.GetAttachmentsAsync(new AttachmentParametersDto(AttachmentType.APPLICATION, application.Id)))[0];
        var newDisplayName = attachment.DisplayName + "_Updated";
        var updateDto = new UpdateAttachmentMetadataDto
        {
            Id = attachment.Id,
            DisplayName = newDisplayName,
            AttachmentType = AttachmentType.APPLICATION
        };

        // Act
        var updatedMetadata = await _attachmentAppServiceTest.UpdateAttachmentMetadataAsync(updateDto);

        // Assert
        updatedMetadata.ShouldNotBeNull();
        updatedMetadata.DisplayName.ShouldBe(newDisplayName);
    }

    [Fact]
    public async Task UpdateAttachmentMetadataAsync_For_Assessment_Should_Update_Metadata()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationsRepository.GetListAsync())[0];
        var assessment = await _assessmentsRepository.FindAsync(s => s.ApplicationId.Equals(application.Id));
        var attachment = (await _attachmentAppServiceTest.GetAttachmentsAsync(new AttachmentParametersDto(AttachmentType.ASSESSMENT, assessment!.Id)))[0];
        var newDisplayName = attachment.DisplayName + "_Updated";
        var updateDto = new UpdateAttachmentMetadataDto
        {
            Id = attachment.Id,
            DisplayName = newDisplayName,
            AttachmentType = AttachmentType.ASSESSMENT
        };

        // Act
        var updatedMetadata = await _attachmentAppServiceTest.UpdateAttachmentMetadataAsync(updateDto);

        // Assert
        updatedMetadata.ShouldNotBeNull();
        updatedMetadata.DisplayName.ShouldBe(newDisplayName);
    }

    [Fact]
    public async Task UpdateAttachmentMetadataAsync_For_Chefs_Should_Update_Metadata()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationsRepository.GetListAsync())[0];
        var attachment = (await _attachmentAppServiceTest.GetApplicationChefsFileAttachmentsAsync(application.Id))[0];
        var newDisplayName = attachment.DisplayName + "_Updated";
        var updateDto = new UpdateAttachmentMetadataDto
        {
            Id = attachment.Id,
            DisplayName = newDisplayName,
            AttachmentType = AttachmentType.CHEFS
        };

        // Act
        var updatedMetadata = await _attachmentAppServiceTest.UpdateAttachmentMetadataAsync(updateDto);

        // Assert
        updatedMetadata.ShouldNotBeNull();
        updatedMetadata.DisplayName.ShouldBe(newDisplayName);
    }

    [Fact]
    public async Task UpdateMetadataInternalAsync_For_Application_Should_Update_Metadata()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationsRepository.GetListAsync())[0];
        var attachment = (await _attachmentAppServiceTest.GetApplicationAsync(application.Id))[0];
        var newDisplayName = attachment.DisplayName + "_Updated";
        var updateDto = new UpdateAttachmentMetadataDto
        {
            Id = attachment.Id,
            DisplayName = newDisplayName,
            AttachmentType = AttachmentType.APPLICATION
        };

        // Act
        var updatedMetadata = await AttachmentAppService.UpdateMetadataInternalAsync(updateDto, _applicationAttachmentRepository, AttachmentType.APPLICATION);

        // Assert
        updatedMetadata.ShouldNotBeNull();
        updatedMetadata.DisplayName.ShouldBe(newDisplayName);
    }

    [Fact]
    public async Task UpdateMetadataInternalAsync_For_Assessment_Should_Update_Metadata()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationsRepository.GetListAsync())[0];
        var assessment = await _assessmentsRepository.FindAsync(s => s.ApplicationId.Equals(application.Id));
        var attachment = (await _attachmentAppServiceTest.GetAssessmentAsync(assessment!.Id))[0];
        var newDisplayName = attachment.DisplayName + "_Updated";
        var updateDto = new UpdateAttachmentMetadataDto
        {
            Id = attachment.Id,
            DisplayName = newDisplayName,
            AttachmentType = AttachmentType.ASSESSMENT
        };

        // Act
        var updatedMetadata = await AttachmentAppService.UpdateMetadataInternalAsync(updateDto, _assessmentAttachmentRepository, AttachmentType.ASSESSMENT);

        // Assert
        updatedMetadata.ShouldNotBeNull();
        updatedMetadata.DisplayName.ShouldBe(newDisplayName);
    }

    [Fact]
    public async Task UpdateMetadataInternalAsync_For_Chefs_Should_Update_Metadata()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationsRepository.GetListAsync())[0];
        var attachment = (await _attachmentAppServiceTest.GetApplicationChefsFileAttachmentsAsync(application.Id))[0];
        var newDisplayName = attachment.DisplayName + "_Updated";
        var updateDto = new UpdateAttachmentMetadataDto
        {
            Id = attachment.Id,
            DisplayName = newDisplayName,
            AttachmentType = AttachmentType.CHEFS
        };

        // Act
        var updatedMetadata = await AttachmentAppService.UpdateMetadataInternalAsync(updateDto, _chefsAttachmentRepository, AttachmentType.CHEFS);

        // Assert
        updatedMetadata.ShouldNotBeNull();
        updatedMetadata.DisplayName.ShouldBe(newDisplayName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateAttachmentMetadataAsync_InvalidType_ThrowsArgumentException()
    {
        // Arrange: Create an update DTO with an invalid AttachmentType value.
        var updateDto = new UpdateAttachmentMetadataDto
        {
            Id = Guid.NewGuid(),
            DisplayName = "Test DisplayName",
            AttachmentType = (AttachmentType)999
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _attachmentAppServiceTest.UpdateAttachmentMetadataAsync(updateDto);
        });
    }
    #endregion UpdateAttachmentMetadataAsync
}

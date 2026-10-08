using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Notifications;
using Unity.Modules.Shared.Permissions;
using Unity.Flex.Domain.Scoresheets;
using Unity.Payments.Domain.PaymentRequests;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.ChangeTracking;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Unity.GrantManager.History;

[Authorize(IdentityConsts.ITOperationsPolicyName)]
public class AuditLogAppService(
    IExtendedAuditLogRepository auditLogRepository,
    IIdentityUserRepository identityUserRepository,
    IApplicationRepository applicationRepository,
    IApplicationFormRepository applicationFormRepository,
    IRepository<Applicant, Guid> applicantRepository,
    IRepository<Assessment, Guid> assessmentRepository,
    IRepository<Intake, Guid> intakeRepository,
    IRepository<ApplicationFormSubmission, Guid> submissionRepository,
    IRepository<ApplicationLink, Guid> applicationLinkRepository,
    IRepository<ApplicationContact, Guid> applicationContactRepository,
    IRepository<ApplicationScoresheetAnswers, Guid> scoresheetAnswersRepository,
    IScoresheetRepository scoresheetRepository,
    IRepository<ApplicationStatus, Guid> applicationStatusRepository,
    IPaymentRequestRepository paymentRequestRepository,
    IRepository<ScheduledNotification, Guid> scheduledNotificationRepository,
    IServiceProvider serviceProvider)
    : GrantManagerAppService, IAuditLogAppService
{
    [DisableEntityChangeTracking]
    public async Task<PagedResultDto<AuditLogDto>> GetListAsync(GetAuditLogsInput input)
    {
        var result = await auditLogRepository.GetEntityChangePageAsync(
            input.StartTime,
            input.EndTime,
            input.EntityTypeFullName,
            input.ChangeType,
            input.ServiceName,
            input.MethodName,
            input.Filter,
            await GetEntityIdsMatchingNameAsync(input.Filter),
            input.SkipCount,
            input.MaxResultCount);

        var users = new Dictionary<Guid, IdentityUser>();
        var applicationIds = result.Items
            .Where(change => change.EntityTypeFullName == typeof(Application).FullName)
            .Select(change => Guid.TryParse(change.EntityId, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        var applications = applicationIds.Length == 0
            ? new List<Application>()
            : await applicationRepository.GetListByIdsAsync(applicationIds);
        var applicationNames = applications.ToDictionary(application => application.Id, application => application.ReferenceNo);
        var relatedEntityNames = await GetRelatedEntityNamesAsync(result.Items);

        var applicationFormIds = result.Items
            .Where(change => change.EntityTypeFullName == typeof(ApplicationForm).FullName)
            .Select(change => Guid.TryParse(change.EntityId, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        var applicationForms = applicationFormIds.Length == 0
            ? new List<ApplicationForm>()
            : await applicationFormRepository.GetListAsync(form => applicationFormIds.Contains(form.Id));
        var applicationFormNames = applicationForms.ToDictionary(form => form.Id, form => form.ApplicationFormName ?? string.Empty);

        var entityDetails = new Dictionary<string, AuditEntityDetails>();
        var applicantIds = GetEntityIds(result.Items, typeof(Applicant));
        AddEntityDetails(entityDetails, typeof(Applicant),
            applicantIds.Length == 0
                ? []
                : await applicantRepository.GetListAsync(applicant => applicantIds.Contains(applicant.Id)),
            applicant => applicant.ApplicantName ?? applicant.OrgName ?? applicant.UnityApplicantId ?? "Applicant",
            applicant => $"/Applicants/Details?ApplicantId={applicant.Id}");
        var intakeIds = GetEntityIds(result.Items, typeof(Intake));
        AddEntityDetails(entityDetails, typeof(Intake),
            intakeIds.Length == 0
                ? []
                : await intakeRepository.GetListAsync(intake => intakeIds.Contains(intake.Id)),
            intake => intake.IntakeName,
            _ => "/Intakes");
        var applicationStatusIds = GetEntityIds(result.Items, typeof(ApplicationStatus));
        AddEntityDetails(entityDetails, typeof(ApplicationStatus),
            applicationStatusIds.Length == 0
                ? []
                : await applicationStatusRepository.GetListAsync(status => applicationStatusIds.Contains(status.Id)),
            status => status.InternalStatus,
            _ => string.Empty);

        var assessmentIds = GetEntityIds(result.Items, typeof(Assessment));
        AddParentApplicationDetails(entityDetails, typeof(Assessment),
            assessmentIds.Length == 0
                ? []
                : await assessmentRepository.GetListAsync(assessment => assessmentIds.Contains(assessment.Id)),
            assessment => $"Assessment ({assessment.Status})", assessment => assessment.ApplicationId);
        var submissionIds = GetEntityIds(result.Items, typeof(ApplicationFormSubmission));
        AddParentApplicationDetails(entityDetails, typeof(ApplicationFormSubmission),
            submissionIds.Length == 0
                ? []
                : await submissionRepository.GetListAsync(submission => submissionIds.Contains(submission.Id)),
            _ => "Form submission", submission => submission.ApplicationId);
        var applicationLinkIds = GetEntityIds(result.Items, typeof(ApplicationLink));
        AddParentApplicationDetails(entityDetails, typeof(ApplicationLink),
            applicationLinkIds.Length == 0
                ? []
                : await applicationLinkRepository.GetListAsync(link => applicationLinkIds.Contains(link.Id)),
            _ => "Application link", link => link.ApplicationId);
        var applicationContactIds = GetEntityIds(result.Items, typeof(ApplicationContact));
        AddParentApplicationDetails(entityDetails, typeof(ApplicationContact),
            applicationContactIds.Length == 0
                ? []
                : await applicationContactRepository.GetListAsync(contact => applicationContactIds.Contains(contact.Id)),
            contact => string.IsNullOrWhiteSpace(contact.ContactFullName) ? "Application contact" : contact.ContactFullName,
            contact => contact.ApplicationId);
        var scoresheetAnswerIds = GetEntityIds(result.Items, typeof(ApplicationScoresheetAnswers));
        AddParentApplicationDetails(entityDetails, typeof(ApplicationScoresheetAnswers),
            scoresheetAnswerIds.Length == 0
                ? []
                : await scoresheetAnswersRepository.GetListAsync(answer => scoresheetAnswerIds.Contains(answer.Id)),
            _ => "Scoresheet answers", answer => answer.ApplicationId);
        var scoresheetIds = GetEntityIds(result.Items, typeof(Scoresheet));
        AddEntityDetails(entityDetails, typeof(Scoresheet),
            scoresheetIds.Length == 0
                ? Enumerable.Empty<Scoresheet>()
                : await scoresheetRepository.GetListByIdsAsync(scoresheetIds),
            scoresheet => string.IsNullOrWhiteSpace(scoresheet.Name) ? scoresheet.Title : scoresheet.Name,
            _ => "/ScoresheetConfiguration");
        var paymentRequestIds = GetEntityIds(result.Items, typeof(PaymentRequest));
        AddEntityDetails(entityDetails, typeof(PaymentRequest),
            paymentRequestIds.Length == 0
                ? []
                : await paymentRequestRepository.GetListAsync(paymentRequest => paymentRequestIds.Contains(paymentRequest.Id)),
            paymentRequest => string.IsNullOrWhiteSpace(paymentRequest.InvoiceNumber) ? "Payment request" : paymentRequest.InvoiceNumber,
            _ => "/PaymentRequests");

        var scheduledNotificationIds = GetEntityIds(result.Items, typeof(ScheduledNotification));
        AddEntityDetails(entityDetails, typeof(ScheduledNotification),
            scheduledNotificationIds.Length == 0
                ? []
                : await scheduledNotificationRepository.GetListAsync(notification => scheduledNotificationIds.Contains(notification.Id)),
            notification => string.IsNullOrWhiteSpace(notification.EventType) ? "Scheduled notification" : notification.EventType,
            _ => "/ApplicationForms");

        var userIds = result.Items
                     .Where(change => change.UserId.HasValue)
                     .Select(change => change.UserId!.Value)
                     .Distinct()
                     .ToArray();
        foreach (var userId in userIds)
        {
            var user = await identityUserRepository.FindAsync(userId);
            if (user != null)
            {
                users[user.Id] = user;
            }
        }

        var items = result.Items.Select(change => new AuditLogDto
        {
            Id = change.EntityChangeId,
            AuditLogId = change.AuditLogId,
            ChangeTime = change.ChangeTime,
            ExecutionTime = change.ExecutionTime,
            EntityTypeFullName = change.EntityTypeFullName,
            EntityId = change.EntityId,
            EntityName = GetEntityName(change, applicationNames, applicationFormNames, entityDetails),
            EntityUrl = GetEntityUrl(change, entityDetails),
            PropertyName = change.PropertyName,
            OriginalValue = GetRelatedEntityName(change.PropertyName, change.OriginalValue, relatedEntityNames),
            NewValue = GetRelatedEntityName(change.PropertyName, change.NewValue, relatedEntityNames),
            ChangeType = change.ChangeType,
            UserName = change.UserName ?? string.Empty,
            UserFirstName = change.UserId.HasValue && users.TryGetValue(change.UserId.Value, out var user)
                ? user.Name
                : string.Empty,
            UserSurname = change.UserId.HasValue && users.TryGetValue(change.UserId.Value, out user)
                ? user.Surname
                : string.Empty,
            TenantName = change.TenantName ?? string.Empty,
            ServiceName = change.ServiceName ?? string.Empty,
            MethodName = change.MethodName ?? string.Empty,
            Url = change.Url ?? string.Empty,
            HttpMethod = change.HttpMethod ?? string.Empty,
            HttpStatusCode = change.HttpStatusCode
        }).ToList();

        return new PagedResultDto<AuditLogDto>(result.TotalCount, items);
    }

    private async Task<IReadOnlyDictionary<string, string>> GetRelatedEntityNamesAsync(
        IEnumerable<AuditLogEntityChange> changes)
    {
        var relatedEntityNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var references = changes
            .Where(change => IsRelationshipProperty(change.PropertyName))
            .SelectMany(change => new[] { change.OriginalValue, change.NewValue }
                .Select(value => new { change.PropertyName, Id = NormalizeGuid(value) }))
            .Where(reference => reference.Id.HasValue)
            .Select(reference => new { reference.PropertyName, Id = reference.Id!.Value })
            .Distinct()
            .ToList();

        foreach (var reference in references)
        {
            var entityType = FindRelatedEntityType(reference.PropertyName);
            if (entityType == null)
            {
                continue;
            }

            var repositoryType = typeof(IRepository<,>).MakeGenericType(entityType, typeof(Guid));
            var repository = serviceProvider.GetService(repositoryType);
            if (repository == null)
            {
                continue;
            }

            dynamic dynamicRepository = repository;
            var relatedEntity = await dynamicRepository.FindAsync(reference.Id);
            var relatedEntityName = GetDisplayValue(relatedEntity, entityType);
            if (!string.IsNullOrWhiteSpace(relatedEntityName))
            {
                relatedEntityNames[GetReferenceKey(reference.PropertyName, reference.Id)] = relatedEntityName;
            }
        }

        return relatedEntityNames;
    }

    private static string GetRelatedEntityName(
        string propertyName,
        string? value,
        IReadOnlyDictionary<string, string> relatedEntityNames)
    {
        var normalizedValue = NormalizeGuid(value);
        return IsRelationshipProperty(propertyName)
            && normalizedValue.HasValue
            && relatedEntityNames.TryGetValue(GetReferenceKey(propertyName, normalizedValue.Value), out var relatedEntityName)
                ? relatedEntityName
                : value ?? string.Empty;
    }

    private static bool IsRelationshipProperty(string? propertyName) =>
        propertyName?.EndsWith("Id", StringComparison.OrdinalIgnoreCase) == true
        && propertyName.Length > 2;

    private static Type? FindRelatedEntityType(string propertyName)
    {
        var entityName = propertyName[..^2];
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(GetLoadableTypes)
            .FirstOrDefault(type =>
                type.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase)
                && typeof(IEntity<Guid>).IsAssignableFrom(type));
    }

    private static IEnumerable<Type> GetLoadableTypes(System.Reflection.Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }

    private static string GetDisplayValue(object? entity, Type entityType)
    {
        if (entity == null)
        {
            return string.Empty;
        }

        var propertyNames = new[]
        {
            "InternalStatus",
            "Name",
            "Title",
            "Description",
            "DisplayName",
            "ReferenceNo",
            "Code",
            "Label",
            "Text",
            "FullName",
            entityType.Name + "Description",
            entityType.Name + "Name"
        };

        foreach (var propertyName in propertyNames)
        {
            var property = entityType.GetProperty(propertyName);
            var value = property?.GetValue(entity)?.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private static string GetReferenceKey(string propertyName, Guid id) =>
        $"{propertyName}:{id}";

    private static Guid? NormalizeGuid(string? value) =>
        Guid.TryParse(value?.Trim().Trim('"'), out var id) ? id : null;

    private static string GetEntityName(
        AuditLogEntityChange change,
        IReadOnlyDictionary<Guid, string> applicationNames,
        IReadOnlyDictionary<Guid, string> applicationFormNames,
        IReadOnlyDictionary<string, AuditEntityDetails> entityDetails)
    {
        if (!Guid.TryParse(change.EntityId, out var entityId))
        {
            return string.Empty;
        }

        if (change.EntityTypeFullName == typeof(Application).FullName
            && applicationNames.TryGetValue(entityId, out var referenceNo))
        {
            return referenceNo;
        }

        if (change.EntityTypeFullName == typeof(ApplicationForm).FullName
            && applicationFormNames.TryGetValue(entityId, out var formName))
        {
            return formName;
        }

        if (entityDetails.TryGetValue(GetEntityKey(change.EntityTypeFullName, entityId), out var details))
        {
            return details.Name;
        }

        return string.Empty;
    }

    private static string GetEntityUrl(
        AuditLogEntityChange change,
        IReadOnlyDictionary<string, AuditEntityDetails> entityDetails)
    {
        if (!Guid.TryParse(change.EntityId, out var entityId) || entityId == Guid.Empty)
        {
            return string.Empty;
        }

        var encodedId = Uri.EscapeDataString(entityId.ToString());
        if (change.EntityTypeFullName == typeof(Application).FullName)
        {
            return $"/GrantApplications/Details?ApplicationId={encodedId}";
        }

        if (change.EntityTypeFullName == typeof(ApplicationForm).FullName)
        {
            return "/ApplicationForms";
        }

        if (entityDetails.TryGetValue(GetEntityKey(change.EntityTypeFullName, entityId), out var details))
        {
            return details.Url;
        }

        return string.Empty;
    }

    private static void AddEntityDetails<T>(
        IDictionary<string, AuditEntityDetails> entityDetails,
        Type entityType,
        IEnumerable<T> entities,
        Func<T, string> name,
        Func<T, string> url)
        where T : IEntity<Guid>
    {
        foreach (var entity in entities)
        {
            entityDetails[GetEntityKey(entityType.FullName!, entity.Id)] = new AuditEntityDetails(name(entity), url(entity));
        }
    }

    private static void AddParentApplicationDetails<T>(
        IDictionary<string, AuditEntityDetails> entityDetails,
        Type entityType,
        IEnumerable<T> entities,
        Func<T, string> name,
        Func<T, Guid> applicationId)
        where T : IEntity<Guid>
    {
        foreach (var entity in entities)
        {
            var parentId = applicationId(entity);
            entityDetails[GetEntityKey(entityType.FullName!, entity.Id)] = new AuditEntityDetails(
                name(entity),
                parentId == Guid.Empty ? string.Empty : $"/GrantApplications/Details?ApplicationId={parentId}");
        }
    }

    private static string GetEntityKey(string entityType, Guid entityId) => $"{entityType}:{entityId}";

    private static Guid[] GetEntityIds(IEnumerable<AuditLogEntityChange> changes, Type entityType)
    {
        return changes
            .Where(change => change.EntityTypeFullName == entityType.FullName)
            .Select(change => Guid.TryParse(change.EntityId, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
    }

    private async Task<IReadOnlyCollection<string>?> GetEntityIdsMatchingNameAsync(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return null;
        }

        var matchingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var applications = await applicationRepository.GetListAsync(application => application.ReferenceNo.Contains(filter));
        var applicationForms = await applicationFormRepository.GetListAsync(form =>
            form.ApplicationFormName != null && form.ApplicationFormName.Contains(filter));
        var applicants = await applicantRepository.GetListAsync(applicant =>
            applicant.ApplicantName != null && applicant.ApplicantName.Contains(filter)
            || applicant.OrgName != null && applicant.OrgName.Contains(filter)
            || applicant.UnityApplicantId != null && applicant.UnityApplicantId.Contains(filter));
        var intakes = await intakeRepository.GetListAsync(intake => intake.IntakeName.Contains(filter));
        var statuses = await applicationStatusRepository.GetListAsync(status => status.InternalStatus.Contains(filter));
        var contacts = await applicationContactRepository.GetListAsync(contact =>
            contact.ContactFullName != null && contact.ContactFullName.Contains(filter));
        var scoresheets = await scoresheetRepository.GetListByNameAsync(filter);
        var paymentRequests = await paymentRequestRepository.GetListAsync(paymentRequest =>
            paymentRequest.InvoiceNumber != null && paymentRequest.InvoiceNumber.Contains(filter));

        foreach (var entity in applications)
        {
            matchingIds.Add($"{typeof(Application).FullName}:{entity.Id}");
        }

        foreach (var entity in applicationForms)
        {
            matchingIds.Add($"{typeof(ApplicationForm).FullName}:{entity.Id}");
        }

        foreach (var entity in applicants)
        {
            matchingIds.Add($"{typeof(Applicant).FullName}:{entity.Id}");
        }

        foreach (var entity in intakes)
        {
            matchingIds.Add($"{typeof(Intake).FullName}:{entity.Id}");
        }

        foreach (var entity in statuses)
        {
            matchingIds.Add($"{typeof(ApplicationStatus).FullName}:{entity.Id}");
        }

        foreach (var entity in contacts)
        {
            matchingIds.Add($"{typeof(ApplicationContact).FullName}:{entity.Id}");
        }

        foreach (var entity in scoresheets)
        {
            matchingIds.Add($"{typeof(Scoresheet).FullName}:{entity.Id}");
        }

        foreach (var entity in paymentRequests)
        {
            matchingIds.Add($"{typeof(PaymentRequest).FullName}:{entity.Id}");
        }

        return matchingIds;
    }

    private sealed record AuditEntityDetails(string Name, string Url);

    [DisableEntityChangeTracking]
    public Task<List<string>> GetEntityTypeFullNamesAsync(GetAuditLogEntityTypesInput input)
    {
        return auditLogRepository.GetEntityTypeFullNamesAsync(input.StartTime, input.EndTime);
    }
}
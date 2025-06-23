using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications;

public interface IApplicantRepository : IRepository<Applicant, Guid>
{
    Task<List<Applicant>> GetUnmatchedApplicantsAsync();
    Task<Applicant?> GetByUnityApplicantIdAsync(string unityApplicantId);
    Task<Applicant?> GetByUnityApplicantNameAsync(string unityApplicantName);
    Task<List<Applicant>> GetApplicantsWithUnityApplicantIdAsync();
    Task<List<Applicant>> GetApplicantsBySiteIdAsync(Guid siteId);
    Task<JsonDocument> GetApplicantAutocompleteQueryAsync(string? applicantLookUpQuery);
}

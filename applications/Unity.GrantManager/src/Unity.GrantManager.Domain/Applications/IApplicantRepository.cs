﻿using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications;

public interface IApplicantRepository : IRepository<Applicant, Guid>
{
    Task<Applicant?> GetByUnityApplicantIdAsync(string unityApplicantId);
    Task<Applicant?> GetByUnityApplicantNameAsync(string unityApplicantName);
}

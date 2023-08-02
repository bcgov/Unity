﻿using System;
using Unity.GrantManager.GrantPrograms;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications;

public interface IApplicationFormSubmissionRepository : IRepository<ApplicationFormSubmission, Guid>
{
}

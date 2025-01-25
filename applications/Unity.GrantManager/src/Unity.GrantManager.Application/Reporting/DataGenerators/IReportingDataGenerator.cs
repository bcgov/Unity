using System;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Reporting.DataGenerators
{
    public interface IReportingDataGenerator : IApplicationService
    {
        string? Generate(dynamic formSubmission, string? reportKeys, Guid submissionId);
    }
}

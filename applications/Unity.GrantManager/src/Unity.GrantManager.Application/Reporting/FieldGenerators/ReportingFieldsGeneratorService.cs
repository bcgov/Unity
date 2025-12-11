using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.BackgroundJobs;

namespace Unity.GrantManager.Reporting.FieldGenerators
{
    [RemoteService(false)]
    public class ReportingFieldsGeneratorService(IApplicationFormRepository applicationFormRepository,
        IBackgroundJobManager backgroundJobManager) : ApplicationService, IReportingFieldsGeneratorService
    {
        /// <summary>
        /// Dissect the form version schema and extract key value pairs to be stored as the base for reporting
        /// </summary>
        /// <param name="applicationFormVersion"></param>
        /// <returns></returns>
        public async Task GenerateAndSetAsync(ApplicationFormVersion applicationFormVersion)
        {
            // Add keys and columns for report generation
            await UpdateFormVersionWithReportKeysAndColumnsAsync(applicationFormVersion);
            await QueueDynamicViewGeneratorAsync(applicationFormVersion);
        }

        private async Task QueueDynamicViewGeneratorAsync(ApplicationFormVersion applicationFormVersion)
        {            
            await backgroundJobManager.EnqueueAsync(
                new SubmissionsDynamicViewGenerationArgs
                {
                    ApplicationFormVersionId = applicationFormVersion.Id,
                    TenantId = CurrentTenant.Id
                },
                BackgroundJobPriority.Normal,
                delay: TimeSpan.FromSeconds(1));
        }

        private async Task UpdateFormVersionWithReportKeysAndColumnsAsync(ApplicationFormVersion applicationFormVersion)
        {
            if (applicationFormVersion.AvailableChefsFields == null) return;

            var form = await applicationFormRepository.GetAsync(applicationFormVersion.ApplicationFormId);
            JObject jObject = JObject.Parse(applicationFormVersion.AvailableChefsFields);

            // Exclusion array
            string[] exclusionArray = ["simplebuttonadvanced", "datagrid", "hidden"];
            string[] nestedKeyFields = ["simplecheckboxes", "simplecheckboxadvanced"];

            // Dictionary to store full key names and truncated key names
            Dictionary<string, string> keyMapping = [];

            // Filter out properties based on the exclusion array and extend child keys
            var keys = jObject
                .Properties()
                .SelectMany(p =>
                {

                    string? typeValue = JObject.Parse(p.Value.ToString())?["type"]?.ToString();
                    if (typeValue != null && exclusionArray.Contains(typeValue))
                    {
                        return [];
                    }

                    // Check for nested key fields and generate dashed keys
                    if (typeValue != null && nestedKeyFields.Contains(typeValue))
                    {
                        return ExtractNestedKeys(p);
                    }

                    return [p.Name];
                })
                .Distinct()
                .Select(fullKey =>
                {
                    string truncatedKey = fullKey.Length > ReportingConsts.ReportColumnMaxLength ? fullKey[..ReportingConsts.ReportColumnMaxLength] : fullKey;
                    keyMapping[fullKey] = truncatedKey;
                    return fullKey;
                });

            // Get all keys and pipe separate them
            string pipeDelimitedKeys = string.Join(ReportingConsts.ReportFieldDelimiter, keys);

            // Truncate each key to a maximum of 63 characters and create a pipe-delimited string
            string truncatedDelimitedKeys = string.Join(ReportingConsts.ReportFieldDelimiter, keys.Select(k => k.Length > ReportingConsts.ReportColumnMaxLength ? k[..ReportingConsts.ReportColumnMaxLength] : k));

            applicationFormVersion.ReportColumns = truncatedDelimitedKeys;
            applicationFormVersion.ReportKeys = pipeDelimitedKeys;
            applicationFormVersion.ReportViewName = $"Form-{form.ApplicationFormName}-V{applicationFormVersion.Version}";
        }

        private static string[] ExtractNestedKeys(JProperty jProperty)
        {
            if (jProperty == null) return [];

            string? valuesProp = JObject.Parse(jProperty.Value.ToString())?["values"]?.ToString();
            return string.IsNullOrEmpty(valuesProp) ? [] : valuesProp.Split(',');
        }
    }
}

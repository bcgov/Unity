using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.Reporting.Configuration;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.Reporting.Web.Views.Shared.Components.ReportingConfiguration
{
    [Widget(
        ScriptTypes = new[] { typeof(ReportingConfigurationScriptBundleContributor) },
        StyleTypes = new[] { typeof(ReportingConfigurationStyleBundleContributor) },
        AutoInitialize = true)]
    public class ReportingConfigurationViewComponent : AbpViewComponent
    {
        private readonly IApplicationFormAppService _applicationFormAppService;
        private readonly IReportMappingService _reportMappingService;

        public ReportingConfigurationViewComponent(
            IApplicationFormAppService applicationFormAppService,
            IReportMappingService reportMappingService)
        {
            _applicationFormAppService = applicationFormAppService;
            _reportMappingService = reportMappingService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid formId, Guid? selectedVersionId = null, string? provider = null)
        {
            // Determine the correlation provider - default to formversion if not specified
            var correlationProvider = !string.IsNullOrEmpty(provider) ? provider : Providers.FormVersion;
            
            // Determine correlation ID based on provider
            Guid? correlationId = null;
            if (correlationProvider == Providers.Scoresheet)
            {
                // For scoresheets, use the form ID directly
                correlationId = formId;
            }
            else
            {
                // For form versions and other providers, use the selected version ID
                correlationId = selectedVersionId;
            }

            var formVersions = await _applicationFormAppService.GetVersionsAsync(formId);

            // For form version provider, use provided selectedVersionId or fall back to first available
            if (correlationProvider == Providers.FormVersion)
            {
                selectedVersionId = selectedVersionId ?? formVersions.FirstOrDefault()?.Id;
                correlationId = selectedVersionId;
            }

            string viewName = string.Empty;
            ViewStatus? viewStatus = null;
            bool hasSavedConfiguration = false;
            bool hasDuplicateKeys = false;

            // If there's a valid correlation ID, try to get the current mapping data
            if (correlationId.HasValue)
            {
                var exists = await _reportMappingService.ExistsAsync(correlationId.Value, correlationProvider);
                hasSavedConfiguration = exists;

                if (exists)
                {
                    var reportColumnsMap = await _reportMappingService
                        .GetByCorrelationAsync(correlationId.Value, correlationProvider);

                    viewName = reportColumnsMap.ViewName;
                    viewStatus = reportColumnsMap.ViewStatus;

                    // Check for duplicate keys in the mapping data
                    hasDuplicateKeys = CheckForDuplicateKeys(reportColumnsMap.Mapping?.Rows);
                }
                else
                {
                    // Check for duplicate keys in the fields metadata (initial load)
                    var fieldsMetadata = await _reportMappingService
                        .GetFieldsMetadataAsync(correlationId.Value, correlationProvider);

                    hasDuplicateKeys = CheckForDuplicateKeys(fieldsMetadata.Fields);
                }
            }

            var model = new ReportingConfigurationViewModel
            {
                FormId = formId,
                FormVersions = [.. formVersions
                    .Select(v => new SelectListItem
                    {
                        Value = v.Id.ToString(),
                        Text = $"{v.Version} - {v.ChefsFormVersionGuid!.ToString()}"
                    })],
                SelectedVersionId = selectedVersionId,
                ViewName = viewName,
                ViewStatus = viewStatus,
                HasSavedConfiguration = hasSavedConfiguration,
                HasDuplicateKeys = hasDuplicateKeys,
                Provider = correlationProvider,
                CorrelationId = correlationId,
                IsVersionSelectorVisible = correlationProvider == Providers.FormVersion
            };

            return View(model);
        }

        /// <summary>
        /// Checks for duplicate keys by looking for (DKx) prefixes in paths
        /// </summary>
        /// <param name="rows">Collection of mapping rows to check</param>
        /// <returns>True if duplicate keys are detected, false otherwise</returns>
        private static bool CheckForDuplicateKeys(IEnumerable<MapRowDto>? rows)
        {
            if (rows == null) return false;

            return rows.Any(row =>
                HasDuplicateKeyPrefix(row.Path) ||
                HasDuplicateKeyPrefix(row.DataPath));
        }

        /// <summary>
        /// Checks for duplicate keys by looking for (DKx) prefixes in field paths
        /// </summary>
        /// <param name="fields">Collection of field metadata to check</param>
        /// <returns>True if duplicate keys are detected, false otherwise</returns>
        private static bool CheckForDuplicateKeys(FieldPathTypeDto[]? fields)
        {
            if (fields == null) return false;

            return fields.Any(field =>
                HasDuplicateKeyPrefix(field.Path) ||
                HasDuplicateKeyPrefix(field.DataPath));
        }

        /// <summary>
        /// Checks if a path contains a duplicate key prefix like (DK1), (DK2), etc.
        /// </summary>
        /// <param name="path">The path to check</param>
        /// <returns>True if the path contains a duplicate key prefix, false otherwise</returns>
        private static bool HasDuplicateKeyPrefix(string? path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            // Check for pattern like (DK1), (DK2), etc. at the beginning of the path
            return System.Text.RegularExpressions.Regex.IsMatch(path, @"^\(DK\d+\)");
        }

        public class ReportingConfigurationStyleBundleContributor : BundleContributor
        {
            public override void ConfigureBundle(BundleConfigurationContext context)
            {
                context.Files
                  .Add("/Views/Shared/Components/ReportingConfiguration/Default.css");
            }
        }

        public class ReportingConfigurationScriptBundleContributor : BundleContributor
        {
            public override void ConfigureBundle(BundleConfigurationContext context)
            {
                context.Files
                  .Add("/libs/pubsub-js/src/pubsub.js");
                context.Files
                  .Add("/themes/ux2/table-utils.js");
                context.Files
                  .Add("/Views/Shared/Components/ReportingConfiguration/Default.js");
            }
        }
    }
}
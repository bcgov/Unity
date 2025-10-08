using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes.Mapping;
using Unity.GrantManager.Integrations.Chefs;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Reporting.Configuration
{
    [RemoteService(false)]
    public class FormMetadataService(IApplicationFormVersionRepository formVersionRepository,
        IFormsApiService formsApiService) : IFormMetadataService, ITransientDependency
    {

        /// <summary>
        /// Retrieves comprehensive metadata for all form components in a specific form version.
        /// This method analyzes the form schema and extracts component information including paths, types, and labels.
        /// It also processes nested components, filters out non-data components, handles duplicate paths, and creates data-centric paths.
        /// </summary>
        /// <param name="formVersionId">The unique identifier of the form version to analyze</param>
        /// <returns>
        /// A FormComponentMetaDataDto containing:
        /// - Components: List of all form components with their metadata
        /// - HasDuplicates: Boolean indicating if duplicate component paths were found and processed
        /// </returns>
        /// <remarks>
        /// This method performs several operations:
        /// 1. Recursively scans all form components including nested structures (panels, tabs, columns, tables)
        /// 2. Expands value options for radio groups and checkboxes into individual entries
        /// 3. Filters out UI-only components (buttons, HTML content, containers)
        /// 4. Handles duplicate paths by prefixing them with (DKx) notation
        /// 5. Creates data-focused paths for easier data access patterns
        /// </remarks>
        public async Task<FormComponentMetaDataDto> GetFormComponentMetaDataAsync(Guid formVersionId)
        {
            (List<FormComponentMetaDataItemDto> items, bool hasDuplicates) = await GetFieldsMetadataItemsAsync(formVersionId);

            var metaData = new FormComponentMetaDataDto
            {
                Components = [.. items],
                HasDuplicates = hasDuplicates
            };

            return metaData;
        }

        /// <summary>
        /// Retrieves metadata for a specific form component identified by its component key within a form version.
        /// This method allows targeted retrieval of component information without processing the entire form schema.
        /// </summary>
        /// <param name="formVersionId">The unique identifier of the form version containing the component</param>
        /// <param name="componentKey">The unique key of the specific component to retrieve metadata for</param>
        /// <returns>
        /// A FormComponentMetaDataItemDto containing the component's metadata if found, or null if the component doesn't exist.
        /// The returned object includes: Id, Key, Type, Label, Path, TypePath, and DataPath properties.
        /// </returns>
        /// <remarks>
        /// This method is currently not implemented and will throw a NotImplementedException when called.
        /// It is intended to provide efficient single-component metadata retrieval without the overhead
        /// of processing the entire form schema.
        /// </remarks>
        /// <exception cref="NotImplementedException">Thrown as this method is not yet implemented</exception>
        public Task<FormComponentMetaDataDto> GetFormComponentMetaDataItemAsync(Guid formVersionId, string componentKey)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Gets simplified metadata for all form components in a synchronous manner
        /// </summary>
        /// <param name="formVersionId">The form version ID</param>
        /// <returns>Array of simplified form component metadata</returns>
        private async Task<(List<FormComponentMetaDataItemDto> items, bool hasDuplicates)> GetFieldsMetadataItemsAsync(Guid formVersionId)
        {
            // Get the form schema
            var formSchemaString = await GetFormSchemaAsync(formVersionId);

            if (string.IsNullOrEmpty(formSchemaString))
            {
                return ([], false);
            }

            try
            {
                var schema = JObject.Parse(formSchemaString);
                var components = schema["components"] as JArray;

                if (components == null)
                {
                    return ([], false);
                }

                var componentsList = new List<FormComponentMetaDataItemDto>();
                ScanComponentsRecursively(components, componentsList, string.Empty, string.Empty);

                FilterOutTypes(componentsList);
                UniqueifyPaths(componentsList, out bool hasDuplicates);
                CreateDataPaths(componentsList);

                return ([.. componentsList], hasDuplicates);
            }
            catch (Exception)
            {
                return ([], false);
            }
        }

        /// <summary>
        /// Filters out non-data components and container components from the components list.
        /// Removes UI elements like buttons, HTML content, and structural containers that don't hold actual form data.
        /// </summary>
        /// <param name="componentsList">The list of components to filter</param>
        private static void FilterOutTypes(List<FormComponentMetaDataItemDto> componentsList)
        {
            // Skip button and other non-data components, as well as container components
            var skipTypes = new HashSet<string>
            {
                // Non-data UI components
                "button",
                "simplebuttonadvanced",
                "html",
                "htmlelement",
                "content",
                "simpleseparator",
                "datagrid",
                "table",
                "tabs",
                "simpletabs",
                "columns",
                "simplecols2",
                "simplecols3",
                "simplecols4",
                "panel",
                "well",
                "fieldset",
                "container",
                "editgrid",
                "form",
                "wizard",
                "selectboxes",
                "radio"
            };

            // Remove components with types that should be skipped
            componentsList.RemoveAll(component => skipTypes.Contains(component.Type));
        }

        /// <summary>
        /// Makes component paths unique by prefixing duplicate paths with (DKx) where x is the duplicate number.
        /// This ensures that components with identical paths can be distinguished for reporting purposes.
        /// </summary>
        /// <param name="componentsList">The list of components to process for unique paths</param>
        /// <param name="hasDuplicates">Output parameter indicating whether any duplicates were found and processed</param>
        private static void UniqueifyPaths(List<FormComponentMetaDataItemDto> componentsList, out bool hasDuplicates)
        {
            // Track path occurrences and their duplicate counters
            var pathCounts = new Dictionary<string, int>();
            var pathCounters = new Dictionary<string, int>();
            hasDuplicates = false;

            // First pass: count occurrences of each path
            foreach (var component in componentsList)
            {
                if (!string.IsNullOrEmpty(component.Path))
                {
                    pathCounts[component.Path] = pathCounts.GetValueOrDefault(component.Path, 0) + 1;
                }
            }

            // Second pass: prefix duplicate paths with (DKx)
            foreach (var component in componentsList)
            {
                if (!string.IsNullOrEmpty(component.Path) && pathCounts[component.Path] > 1)
                {
                    // Get the current counter for this path
                    pathCounters[component.Path] = pathCounters.GetValueOrDefault(component.Path, 0) + 1;
                    int duplicateNumber = pathCounters[component.Path];

                    // Prefix with (DKx) where x is the duplicate number
                    component.Path = $"(DK{duplicateNumber}){component.Path}";
                    hasDuplicates = true;
                }
            }
        }

        /// <summary>
        /// Creates data-centric paths for components, providing an alternative path focused on data access patterns.
        /// This method transforms the component paths into a format optimized for data retrieval and reporting.
        /// </summary>
        /// <param name="componentsList">The list of components to process for data paths</param>
        private static void CreateDataPaths(List<FormComponentMetaDataItemDto> componentsList)
        {
            foreach (var component in componentsList)
            {
                component.DataPath = CreateDataPath(component.Path, component.Type);
            }
        }

        /// <summary>
        /// Creates a data-centric path from a component path by removing container elements that don't appear in the data structure
        /// </summary>
        /// <param name="path">The original component path</param>
        /// <param name="componentType">The type of the component</param>
        /// <returns>The data-centric path</returns>
        private static string CreateDataPath(string path, string componentType)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            // Handle duplicate key prefixes (DKx) - preserve them in data path
            string duplicatePrefix = string.Empty;
            string workingPath = path;

            if (path.StartsWith("(DK") && path.Contains(")"))
            {
                int endIndex = path.IndexOf(")");
                duplicatePrefix = path.Substring(0, endIndex + 1);
                workingPath = path.Substring(endIndex + 1);
            }

            // Split the path into segments
            var segments = workingPath.Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries);
            var dataPathSegments = new List<string>();

            // Container types that should be filtered out from data paths
            var containerTypes = new HashSet<string>
            {
                "panel", "well", "fieldset", "container", "tabs", "simpletabs",
                "columns", "simplecols2", "simplecols3", "simplecols4",
                "form", "wizard"
            };

            // Process each segment to determine if it should be included in the data path
            foreach (var segment in segments)
            {
                // Always include segments that don't look like container segments
                if (!IsLikelyContainerSegment(segment))
                {
                    dataPathSegments.Add(segment);
                }
            }

            // If we filtered out everything, preserve the last segment as it's likely the data key
            if (dataPathSegments.Count == 0 && segments.Length > 0)
            {
                dataPathSegments.Add(segments.Last());
            }

            // Reconstruct the data path
            var dataPath = string.Join("->", dataPathSegments);

            // Add back the duplicate prefix if it existed
            if (!string.IsNullOrEmpty(duplicatePrefix))
            {
                dataPath = duplicatePrefix + dataPath;
            }

            return dataPath;
        }

        /// <summary>
        /// Determines if a path segment is likely a container element that won't appear in the data structure
        /// </summary>
        /// <param name="segment">The path segment to evaluate</param>
        /// <returns>True if the segment is likely a container</returns>
        private static bool IsLikelyContainerSegment(string segment)
        {
            if (string.IsNullOrEmpty(segment))
                return false;

            // Common container naming patterns
            var containerPatterns = new[]
            {
                "panel", "tab", "tabs", "section", "group", "container", "wrapper",
                "fieldset", "well", "columns", "cols", "layout", "form"
            };

            var lowerSegment = segment.ToLowerInvariant();

            // Check if the segment contains common container words or is exactly "tabs"
            return containerPatterns.Any(pattern => lowerSegment.Contains(pattern)) || lowerSegment == "tabs";
        }

        /// <summary>
        /// Gets the form schema string from either the database or CHEFS API
        /// </summary>
        /// <param name="formVersionId">The form version ID</param>
        /// <returns>The form schema JSON string</returns>
        private async Task<string?> GetFormSchemaAsync(Guid formVersionId)
        {
            var formVersion = await formVersionRepository.GetAsync(formVersionId);

            if (formVersion.FormSchema != null)
            {
                return formVersion.FormSchema;
            }

            if (formVersion.ChefsApplicationFormGuid == null || formVersion.ChefsFormVersionGuid == null)
            {
                throw new UserFriendlyException("Error with Form configuration");
            }

            // Call off to CHEFS to get the schema
            dynamic? formSchemaResponse = await formsApiService.GetFormDataAsync(
                formVersion.ChefsApplicationFormGuid,
                formVersion.ChefsFormVersionGuid);

            if (formSchemaResponse == null || formSchemaResponse?.schema == null)
            {
                throw new UserFriendlyException("Error with Form configuration");
            }

            JObject schema = (JObject)(formSchemaResponse!.schema);
            return ChefsFormIOReplacement.ReplaceAdvancedFormIoControls(schema);
        }

        /// <summary>
        /// Recursively scans all components in the form schema
        /// </summary>
        /// <param name="components">The components array to scan</param>
        /// <param name="componentsList">The list to populate with found components</param>
        /// <param name="currentPath">The current path using keys</param>
        /// <param name="currentTypePath">The current path using types</param>
        private static void ScanComponentsRecursively(JArray components, List<FormComponentMetaDataItemDto> componentsList,
            string currentPath, string currentTypePath)
        {
            foreach (var component in components.OfType<JObject>())
            {
                ProcessSingleComponent(component, componentsList, currentPath, currentTypePath);
            }
        }

        /// <summary>
        /// Processes a single component and recursively processes its nested components
        /// </summary>
        /// <param name="component">The component to process</param>
        /// <param name="componentsList">The list to populate with found components</param>
        /// <param name="currentPath">The current path using keys</param>
        /// <param name="currentTypePath">The current path using types</param>
        private static void ProcessSingleComponent(JObject component, List<FormComponentMetaDataItemDto> componentsList,
            string currentPath, string currentTypePath)
        {
            var id = component["id"]?.ToString() ?? string.Empty;
            var key = component["key"]?.ToString() ?? string.Empty;
            var type = component["type"]?.ToString() ?? string.Empty;
            var label = component["label"]?.ToString();

            // Skip components without key or type
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(type))
            {
                return;
            }

            // Build the paths
            var newPath = string.IsNullOrEmpty(currentPath) ? key : $"{currentPath}->{key}";
            var newTypePath = string.IsNullOrEmpty(currentTypePath) ? type : $"{currentTypePath}->{type}";

            // Add the current component to the list
            componentsList.Add(new FormComponentMetaDataItemDto
            {
                Id = id,
                Key = key,
                Type = type,
                Label = label,
                Path = newPath,
                TypePath = newTypePath
            });

            // Process expanded value options if applicable
            if (ShouldExpandValueOptions(type) && HasValuesArray(component))
            {
                ProcessExpandedValueOptions(component, componentsList, key, newPath, newTypePath);
            }

            // Process nested components based on component type
            ProcessNestedComponents(component, componentsList, newPath, newTypePath);
        }

        /// <summary>
        /// Processes nested components based on the component type
        /// </summary>
        /// <param name="component">The parent component</param>
        /// <param name="componentsList">The list to populate with found components</param>
        /// <param name="currentPath">The current path using keys</param>
        /// <param name="currentTypePath">The current path using types</param>
        private static void ProcessNestedComponents(JObject component, List<FormComponentMetaDataItemDto> componentsList,
            string currentPath, string currentTypePath)
        {
            var type = component["type"]?.ToString();

            // Handle different types of nested component structures
            switch (type)
            {
                case "table":
                    ProcessTableComponent(component, componentsList, currentPath, currentTypePath);
                    break;

                case "columns":
                case "simplecols2":
                case "simplecols3":
                case "simplecols4":
                    ProcessColumnsComponent(component, componentsList, currentPath, currentTypePath);
                    break;

                case "tabs":
                case "simpletabs":
                    ProcessTabsComponent(component, componentsList, currentPath, currentTypePath);
                    break;

                default:
                    // Standard components property
                    ProcessStandardNestedComponents(component, componentsList, currentPath, currentTypePath);
                    break;
            }
        }

        /// <summary>
        /// Processes table components which store nested components in rows
        /// </summary>
        /// <param name="component">The table component</param>
        /// <param name="componentsList">The list to populate with found components</param>
        /// <param name="currentPath">The current path using keys</param>
        /// <param name="currentTypePath">The current path using types</param>
        private static void ProcessTableComponent(JObject component, List<FormComponentMetaDataItemDto> componentsList,
            string currentPath, string currentTypePath)
        {
            var rows = component["rows"] as JArray;
            if (rows != null)
            {
                foreach (var row in rows.OfType<JArray>())
                {
                    ScanComponentsRecursively(row, componentsList, currentPath, currentTypePath);
                }
            }
        }

        /// <summary>
        /// Processes column components which store nested components in columns
        /// </summary>
        /// <param name="component">The columns component</param>
        /// <param name="componentsList">The list to populate with found components</param>
        /// <param name="currentPath">The current path using keys</param>
        /// <param name="currentTypePath">The current path using types</param>
        private static void ProcessColumnsComponent(JObject component, List<FormComponentMetaDataItemDto> componentsList,
            string currentPath, string currentTypePath)
        {
            var columns = component["columns"] as JArray;
            if (columns != null)
            {
                foreach (var column in columns.OfType<JObject>())
                {
                    var columnComponents = column["components"] as JArray;
                    if (columnComponents != null)
                    {
                        ScanComponentsRecursively(columnComponents, componentsList, currentPath, currentTypePath);
                    }
                }
            }
        }

        /// <summary>
        /// Processes tabs components which store nested components in individual tabs
        /// </summary>
        /// <param name="component">The tabs component</param>
        /// <param name="componentsList">The list to populate with found components</param>
        /// <param name="currentPath">The current path using keys</param>
        /// <param name="currentTypePath">The current path using types</param>
        private static void ProcessTabsComponent(JObject component, List<FormComponentMetaDataItemDto> componentsList,
            string currentPath, string currentTypePath)
        {
            var components = component["components"] as JArray;
            if (components != null)
            {
                foreach (var tab in components.OfType<JObject>())
                {
                    var tabComponents = tab["components"] as JArray;
                    if (tabComponents != null)
                    {
                        ScanComponentsRecursively(tabComponents, componentsList, currentPath, currentTypePath);
                    }
                }
            }
        }

        /// <summary>
        /// Processes components with standard nested components property
        /// </summary>
        /// <param name="component">The parent component</param>
        /// <param name="componentsList">The list to populate with found components</param>
        /// <param name="currentPath">The current path using keys</param>
        /// <param name="currentTypePath">The current path using types</param>
        private static void ProcessStandardNestedComponents(JObject component, List<FormComponentMetaDataItemDto> componentsList,
            string currentPath, string currentTypePath)
        {
            var nestedComponents = component["components"] as JArray;
            if (nestedComponents != null)
            {
                ScanComponentsRecursively(nestedComponents, componentsList, currentPath, currentTypePath);
            }
        }

        /// <summary>
        /// Processes expanded value options for components like radio groups and checkbox groups
        /// </summary>
        /// <param name="component">The component with values to expand</param>
        /// <param name="componentsList">The list to populate with found components</param>
        /// <param name="key">The component key</param>
        /// <param name="currentPath">The current path using keys</param>
        /// <param name="currentTypePath">The current path using types</param>
        private static void ProcessExpandedValueOptions(JObject component, List<FormComponentMetaDataItemDto> componentsList,
            string key, string currentPath, string currentTypePath)
        {
            var values = component["values"] as JArray;
            if (values != null)
            {
                foreach (var value in values.OfType<JObject>())
                {
                    var optionValue = value["value"]?.ToString();
                    var optionLabel = value["label"]?.ToString();

                    if (!string.IsNullOrEmpty(optionValue))
                    {
                        var optionKey = $"{key}-{optionValue}";
                        var optionPath = $"{currentPath}->{optionValue}";
                        var optionTypePath = $"{currentTypePath}->option";

                        componentsList.Add(new FormComponentMetaDataItemDto
                        {
                            Id = $"{component["id"]?.ToString() ?? string.Empty}-{optionValue}",
                            Key = optionKey,
                            Type = "option",
                            Label = optionLabel ?? optionValue,
                            Path = optionPath,
                            TypePath = optionTypePath
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Determines if value options should be expanded into individual entries
        /// Radio groups and checkbox groups are expanded to create individual entries for each option
        /// </summary>
        /// <param name="type">Component type</param>
        /// <returns>True if options should be expanded</returns>
        private static bool ShouldExpandValueOptions(string? type)
        {
            if (string.IsNullOrEmpty(type))
                return false;

            var expandTypes = new HashSet<string>
            {
                // Checkbox types
                "simplecheckboxes",
                "simplecheckboxadvanced",
                "selectboxes",
                // Radio types - expanded like checkboxes for reporting
                "radio",
                "simpleradios",
                "simpleradioadvanced"
            };

            return expandTypes.Contains(type);
        }

        /// <summary>
        /// Determines if a component has a values array
        /// </summary>
        /// <param name="component">The component to check</param>
        /// <returns>True if component has values array</returns>
        private static bool HasValuesArray(JObject component)
        {
            return component["values"] is JArray values && values.Count > 0;
        }
    }
}
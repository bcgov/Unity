using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Flex.Worksheets.Definitions;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.Flex.Web.Views.Shared.Components.DataGridDefinitionWidget
{
    [ViewComponent(Name = "DataGridDefinitionWidget")]
    [Widget(RefreshUrl = "../Flex/Widgets/DataGridDefinition/Refresh",
        ScriptTypes = [typeof(DataGridDefinitionWidgetScriptBundleContributor)],
        StyleTypes = [typeof(DataGridDefinitionWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public partial class DataGridDefinitionWidget : AbpViewComponent
    {
        const string validInputPattern = @"^[ə̀ə̩ə̥ɛæə̌ə̂ə̧ə̕ə̓ᵒə̄ə̱·ʷəŧⱦʸʋɨⱡɫʔʕⱥɬθᶿɣɔɩłə̈ʼə̲ᶻꭓȼƛλŋƚə̨ə̣ə́ `1234567890-=qwertyuiop[]asdfghjkl;_'_\\zxcvbnm,.~!@#$%^&*()_+QWERTYUIOP{}ASDFGHJKL:""||ZXCVBNM<>?]+$";

        [GeneratedRegex(validInputPattern)]
        protected static partial Regex ValidColumnNames();

        internal static object? ParseFormValues(IFormCollection form)
        {
            var dataGridDefinition = new DataGridDefinition();

            var dynamic = form["Dynamic"].ToString().IsTruthy();
            var summaryOption = form["SummaryOption"].ToString();
            var columnKeys = form["ColumnKeys"];
            var columnTypes = form["ColumnTypes"];

            dataGridDefinition.Dynamic = dynamic;
            dataGridDefinition.SummaryOption = summaryOption;
            dataGridDefinition.Columns = ValidateAndGenerateColumns(dynamic, columnKeys, columnTypes);

            return dataGridDefinition;
        }

        private static List<DataGridDefinitionColumn> ValidateAndGenerateColumns(bool dynamic, StringValues columnKeys, StringValues columnTypes)
        {
            var dataGridsDefinitionColumns = new List<DataGridDefinitionColumn>();

            if (!dynamic && (columnKeys.Count == 0 || columnTypes.Count == 0))
            {
                throw new UserFriendlyException($"Columns are required for non-dynamic table");
            }

            if (columnKeys.Distinct().Count() != columnKeys.Count)
            {
                throw new UserFriendlyException("Column names must be unique");
            }

            var indx = 0;
            foreach (var column in columnKeys)
            {
                if (string.IsNullOrWhiteSpace(column))
                {
                    throw new UserFriendlyException("There are empty column names captured which are required");
                }

                if (!IsValidInput(column))
                {
                    throw new UserFriendlyException("The following characters are allowed for Keys: " + validInputPattern);
                }

                dataGridsDefinitionColumns.Add(new DataGridDefinitionColumn() { Name = column, Type = columnTypes[indx] ?? "Invalid" });

                indx++;
            }

            return dataGridsDefinitionColumns;
        }

        protected static bool IsValidInput(string input)
        {
            Regex regex = ValidColumnNames();
            return regex.IsMatch(input);
        }

        public async Task<IViewComponentResult> InvokeAsync(string? definition)
        {
            if (definition != null)
            {
                DataGridDefinition? dataGridDefinition = JsonSerializer.Deserialize<DataGridDefinition>(definition);
                if (dataGridDefinition != null)
                {
                    return View(await Task.FromResult(new DataGridDefinitionViewModel()
                    {
                        Dynamic = dataGridDefinition.Dynamic,
                        Columns = dataGridDefinition.Columns,
                        Type = Flex.Worksheets.CustomFieldType.DataGrid,
                        Definition = definition,
                        SummaryOption = dataGridDefinition.SummaryOption
                    }));
                }
            }

            return View(await Task.FromResult(new DataGridDefinitionViewModel()));
        }
    }

    public class DataGridDefinitionWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/DataGridDefinitionWidget/Default.css");
        }
    }

    public class DataGridDefinitionWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
                .AddIfNotContains("/Views/Shared/Components/Common/KeyValueComponents.js");

            context.Files
              .AddIfNotContains("/Views/Shared/Components/DataGridDefinitionWidget/Default.js");
        }
    }
}

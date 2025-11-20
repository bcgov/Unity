using System.Collections.Generic;

namespace Unity.Flex.Reporting.Configuration
{
    public class WorksheetComponentMetaDataDto
    {
        /// <summary>
        /// List of components metadata in the worksheet
        /// </summary>
        public List<WorksheetComponentMetaDataItemDto> Components { get; set; } = [];
    }

    public class WorksheetComponentMetaDataItemDto
    {
        /// <summary>
        /// The component ID from the worksheet field schema
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The component key from the form schema
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// The component type (e.g., "textfield", "radio", "datagrid")
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// The component label
        /// </summary>
        public string? Label { get; set; }

        /// <summary>
        /// The path to reach this component using keys (e.g., "datagrid1->field1", "panel1->text1")
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// The path to reach this component using types (e.g., "datagrid->textarea", "panel->textfield")
        /// </summary>
        public string TypePath { get; set; } = string.Empty;

        /// <summary>
        /// The path to reach the data, this is a datacentric version of the Path, and could be the same
        /// </summary>
        public string DataPath { get; set; } = string.Empty;
    }
}

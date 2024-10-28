using System.Collections.Generic;

namespace Unity.Flex.Worksheets.Definitions
{
    public class DataGridDefinition : CustomFieldDefinition
    {
        public DataGridDefinition() : base()
        {
        }

        public bool IsDynamic { get; set; }

        // Placeholder for now, these will be blank for dyamic datagrids
        public List<string> Columns { get; set; } = [];
        public List<string> Rows { get; set; } = [];
    }
}

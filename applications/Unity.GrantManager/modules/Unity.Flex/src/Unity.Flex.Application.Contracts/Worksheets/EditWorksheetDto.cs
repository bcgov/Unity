using System;

namespace Unity.Flex.Worksheets
{
    public class EditWorksheetDto
    {
        [Serializable]
        public sealed class CreateWorksheetDto
        {
            public string Title { get; set; } = string.Empty;            
        }
    }
}

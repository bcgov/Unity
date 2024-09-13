using System;

namespace Unity.Flex.Scoresheets
{
    [Serializable]
    public class EditScoresheetDto
    {
        public string Title { get; set; } = string.Empty;
        public string ActionType {  get; set; } = string.Empty;
    }
}

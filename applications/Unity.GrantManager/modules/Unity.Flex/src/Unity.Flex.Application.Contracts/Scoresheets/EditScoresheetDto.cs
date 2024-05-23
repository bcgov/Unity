using System;

namespace Unity.Flex.Scoresheets
{
    [Serializable]
    public class EditScoresheetDto
    {
        public string Name { get; set; } = string.Empty;
        public Guid ScoresheetId { get; set; }
        public Guid GroupId { get; set; }
        public string ActionType {  get; set; } = string.Empty;
    }
}

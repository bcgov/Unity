using System;

namespace Unity.Flex.Scoresheets
{
    [Serializable]
    public class EditScoresheetDto
    {
        public string Name { get; set; } = string.Empty;
        public Guid ScoresheetId { get; set; }
    }
}

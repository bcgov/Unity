using System;

namespace Unity.Flex.Scoresheets
{
    [Serializable]
    public class CreateScoresheetDto
    {
        public string Title { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}

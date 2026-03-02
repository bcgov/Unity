using System.Collections.Generic;

namespace Unity.GrantManager.AI
{
    public class ScoresheetSectionResponse
    {
        public Dictionary<string, ScoresheetSectionAnswer> Answers { get; set; } = new();
    }
}

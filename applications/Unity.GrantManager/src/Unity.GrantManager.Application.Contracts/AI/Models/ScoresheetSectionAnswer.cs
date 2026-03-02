using System.Text.Json;

namespace Unity.GrantManager.AI
{
    public class ScoresheetSectionAnswer
    {
        public JsonElement Answer { get; set; }
        public string Rationale { get; set; } = string.Empty;
        public int Confidence { get; set; }
    }
}

using System.ComponentModel;

namespace Unity.GrantManager.Integrations
{
    public class CreateUpdateDynamicUrlDto
    {
        [DisplayName("Key Name")]
        public string KeyName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}

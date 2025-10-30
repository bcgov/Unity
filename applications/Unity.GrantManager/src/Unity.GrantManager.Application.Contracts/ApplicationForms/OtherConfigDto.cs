using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.ApplicationForms
{
    public class OtherConfigDto
    {
        public bool IsDirectApproval { get; set; }
        public AddressType? ElectoralDistrictAddressType { get; set; }
        public string? Prefix { get; set; }
        public SuffixConfigType? SuffixType { get; set; }

    }
}

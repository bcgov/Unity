namespace Unity.GrantManager.Data
{
    public class ApplicantLookup
    {        
        public string? UnityApplicantId { get; set; }
        public string? UnityApplicantName { get; set; }
        public bool CreateIfNotExists { get; set; } = false;
    }
}

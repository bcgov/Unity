using Newtonsoft.Json;
using System.Collections.Generic;

namespace Unity.GrantManager.GrantApplications
{
    public class BulkApprovalResultDto
    {        
        public List<string> Successes { get; set; } = [];

        [JsonProperty("failures")]
        public List<KeyValuePair<string, string>> Failures { get; set; } = [];
    }
}

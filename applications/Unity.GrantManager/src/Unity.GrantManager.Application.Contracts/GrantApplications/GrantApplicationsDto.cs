using System.Collections.Generic;

namespace Unity.GrantManager.GrantApplications
{
    public class GrantApplicationsDto
    {
        public int RecordsTotal { get; set; }
        public int RecordsFiltered { get; set; }
        public int Draw { get; set; }
        public List<GrantApplicationDto> Data { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unity.GrantManager.Applicants
{
    public class UpdateApplicantIdDto
    {
        public Guid ApplicationId { get; set; }
        public Guid ApplicantId { get; set; }
    }
}

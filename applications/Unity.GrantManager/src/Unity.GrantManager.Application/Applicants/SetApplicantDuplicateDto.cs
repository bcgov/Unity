using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unity.GrantManager.Applicants
{
    public class SetApplicantDuplicateDto
    {
        public Guid PrincipalApplicantId { get; set; }
        public Guid NonPrincipalApplicantId { get; set; }
    }
}

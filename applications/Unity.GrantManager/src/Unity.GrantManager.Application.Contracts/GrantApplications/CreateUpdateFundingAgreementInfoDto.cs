using System;
using Unity.Flex.Worksheets;

namespace Unity.GrantManager.GrantApplications
{
    public class CreateUpdateFundingAgreementInfoDto : CustomDataFieldDto
    {
        public Guid? ApplicationId { get; set; }
        public string? ContractNumber { get; set; }
        public DateTime? ContractExecutionDate { get; set; }
    }
}

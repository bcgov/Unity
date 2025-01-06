using System.Threading.Tasks;
using Unity.GrantManager.Applicants;
using Unity.Payments.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace Unity.GrantManager.Handlers
{
    public class SupplierCreatedHandler(IApplicantAppService applicantsService) :
                    ILocalEventHandler<ApplicantSupplierEto>, ITransientDependency
    {
        public async Task HandleEventAsync(ApplicantSupplierEto applicantSupplierEto)
        {
            await applicantsService.RelateSupplierToApplicant(applicantSupplierEto);
        }
    }
}

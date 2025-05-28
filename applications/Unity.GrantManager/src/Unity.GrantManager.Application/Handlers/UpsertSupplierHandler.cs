using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applicants;
using Unity.GrantManager.Applications;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Events;
using Unity.Payments.PaymentRequests;
using Unity.Payments.Suppliers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace Unity.GrantManager.Handlers
{
    public class SupplierCreatedHandler(IApplicantAppService applicantsService,
                                        ISiteAppService siteAppService,
                                        IPaymentRequestAppService paymentRequestAppService) :
                                        ILocalEventHandler<ApplicantSupplierEto>, ITransientDependency
    {
        public async Task HandleEventAsync(ApplicantSupplierEto applicantSupplierEto)
        {
            await applicantsService.RelateSupplierToApplicant(applicantSupplierEto);
            Dictionary<string, Site>? existingSitesDictionary = applicantSupplierEto.ExistingSitesDictionary;
            List<SiteEto> newSites = applicantSupplierEto.SiteEtos ??= new List<SiteEto>();

            // Handle deletions
            var sitesToDelete = existingSitesDictionary?.Keys.Except(newSites.Select(s => s.SupplierSiteCode)) ?? Enumerable.Empty<string>();

            foreach (var siteNumber in sitesToDelete)
            {
                if (existingSitesDictionary == null) continue;
                Guid siteId = existingSitesDictionary[siteNumber].Id;
                // First check if the site is being used
                // Get all Applicants with the site id
                List<Applicant> applicants = existingSitesDictionary != null
                    ? await applicantsService.GetApplicantsBySiteIdAsync(siteId)
                    : new List<Applicant>();

                if (applicants.Count > 0)
                {
                    // The site should be deleted but is associate with an applicant as the default site
                    // Mark the site as should be deleted MarkDeletedInUse
                    await siteAppService.MarkDeletedInUseAsync(siteId);
                    continue;
                }

                // Get all Payment Requests with the site id
                int paymentRequests = await paymentRequestAppService.GetPaymentRequestCountBySiteIdAsync(siteId);
                if (paymentRequests > 0)
                {
                    // The site should be deleted but is associate with a payment request
                    // Mark the site as should be deleted MarkDeletedInUse
                    await siteAppService.MarkDeletedInUseAsync(siteId);
                    continue;
                }
                // Delete the site
                await siteAppService.DeleteAsync(siteId);
            }
        }
    }
}

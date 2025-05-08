using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Unity.Payments.Domain.PaymentTags;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Unity.Payments.PaymentTags
{
    [RequiresFeature("Unity.Payments")]
    [Authorize]
    public class PaymentTagAppService : PaymentsAppService, IPaymentTagAppService
    {
        private readonly IPaymentTagRepository _paymentTagRepository;
        public PaymentTagAppService(IPaymentTagRepository paymentTagRepository)
        {
            _paymentTagRepository = paymentTagRepository;
        }
        public async Task<IList<PaymentTagDto>> GetListAsync()
        {
            var paymentTags = await _paymentTagRepository.GetListAsync();
            return ObjectMapper.Map<List<PaymentTag>, List<PaymentTagDto>>(paymentTags.OrderBy(t => t.Id).ToList());
        }
        public async Task<IList<PaymentTagDto>> GetListWithPaymentRequestIdsAsync(List<Guid> ids)
        {
            var tags = await _paymentTagRepository.GetListAsync(e => ids.Contains(e.PaymentRequestId));

            return ObjectMapper.Map<List<PaymentTag>, List<PaymentTagDto>>(tags.OrderBy(t => t.Id).ToList());
        }
        public async Task<PaymentTagDto?> GetPaymentTagsAsync(Guid id)
        {
            var paymentTags = await _paymentTagRepository.FirstOrDefaultAsync(s => s.PaymentRequestId == id);

            if (paymentTags == null) return null;

            return ObjectMapper.Map<PaymentTag, PaymentTagDto>(paymentTags);
        }
        public async Task<PaymentTagDto> CreateorUpdateTagsAsync(Guid id, PaymentTagDto input)
        {
            var paymentTag = await _paymentTagRepository.FirstOrDefaultAsync(e => e.PaymentRequestId == id);

            if (paymentTag == null)
            {
                var newTag = await _paymentTagRepository.InsertAsync(new PaymentTag(
                        Guid.NewGuid(), // Generate a new ID for the PaymentTag  
                        input.PaymentRequestId,
                        input.Text
                    ),
                    autoSave: true
                );

                return ObjectMapper.Map<PaymentTag, PaymentTagDto>(newTag);
            }
            else
            {
                paymentTag.Text = input.Text;
                await _paymentTagRepository.UpdateAsync(paymentTag, autoSave: true);
                return ObjectMapper.Map<PaymentTag, PaymentTagDto>(paymentTag);
            }
        }

    }
}

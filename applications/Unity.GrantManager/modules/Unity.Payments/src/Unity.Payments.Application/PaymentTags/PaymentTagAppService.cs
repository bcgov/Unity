using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Permissions;
using Unity.Payments.Domain.PaymentTags;
using Volo.Abp.Application.Dtos;
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

            // Sanitize input tag text string
            var tagInput = input.Text.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
            input.Text = string.Join(',', tagInput.OrderBy(t => t, StringComparer.InvariantCultureIgnoreCase));

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

        [Authorize(UnitySettingManagementPermissions.Tags.Default)]
        public async Task<PagedResultDto<TagSummaryCountDto>> GetTagSummaryAsync()
        {
            var tagSummary = ObjectMapper.Map<List<TagSummaryCount>, List<TagSummaryCountDto>>(
            await _paymentTagRepository.GetTagSummary());

            return new PagedResultDto<TagSummaryCountDto>(
                tagSummary.Count,
                tagSummary
            );
        }

        [Authorize(UnitySettingManagementPermissions.Tags.Update)]
        public Task<List<Guid>> RenameTagAsync(string originalTag, string replacementTag)
        {
            throw new NotImplementedException();
        }

        [Authorize(UnitySettingManagementPermissions.Tags.Delete)]
        public Task DeleteTagAsync(string deleteTag)
        {
            throw new NotImplementedException();
        }
    }
}

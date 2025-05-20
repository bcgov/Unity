using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Modules.Shared;
using Unity.Payments.Domain.PaymentTags;
using Volo.Abp;
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
            var tagInput = input.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();
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

        [Authorize(UnitySelector.SettingManagement.Tags.Default)]
        public async Task<PagedResultDto<TagSummaryCountDto>> GetTagSummaryAsync()
        {
            var tagSummary = ObjectMapper.Map<List<TagSummaryCount>, List<TagSummaryCountDto>>(
            await _paymentTagRepository.GetTagSummary());

            return new PagedResultDto<TagSummaryCountDto>(
                tagSummary.Count,
                tagSummary
            );
        }

        /// <summary>
        /// For a given Tag, finds the maximum length available for renaming.
        /// </summary>
        /// <param name="originalTag">The tag to be replaced.</param>
        /// <returns>The maximum length available for renaming</returns>
        [Authorize(UnitySelector.SettingManagement.Tags.Update)]
        public async Task<int> GetMaxRenameLengthAsync(string originalTag)
        {
            Check.NotNullOrWhiteSpace(originalTag, nameof(originalTag));
            return await _paymentTagRepository.GetMaxRenameLengthAsync(originalTag);
        }

        [Authorize(UnitySelector.SettingManagement.Tags.Update)]
        public async Task<List<Guid>> RenameTagAsync(string originalTag, string replacementTag)
        {
            Check.NotNullOrWhiteSpace(originalTag, nameof(originalTag));
            Check.NotNullOrWhiteSpace(replacementTag, nameof(replacementTag));

            // Remove commas and trim whitespace from tags
            originalTag = originalTag.Replace(",", string.Empty).Trim();
            replacementTag = replacementTag.Replace(",", string.Empty).Trim();

            if (string.Equals(originalTag, replacementTag, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new BusinessException("Cannot update a tag to itself.");
            }

            var paymentRequestTags = await _paymentTagRepository
                .GetListAsync(e => e.Text.Contains(originalTag));

            if (paymentRequestTags.Count == 0)
                return [];

            int maxRemainingLength = await GetMaxRenameLengthAsync(originalTag);
            if (replacementTag.Length > maxRemainingLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(replacementTag),
                    $"String length exceeds maximum allowed length of {maxRemainingLength}. Actual length: {replacementTag.Length}"
                );
            }

            var updatedTags = new List<PaymentTag>(paymentRequestTags.Count);

            foreach (var item in paymentRequestTags)
            {
                // Split and trim tags, use case-insensitive HashSet for matching
                var tagSet = new HashSet<string>(
                item.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                    StringComparer.InvariantCultureIgnoreCase);

                // Only replace if the original tag exists (case-insensitive)
                if (tagSet.Remove(originalTag))
                {
                    tagSet.Add(replacementTag); // No effect if replacement already exists
                    item.Text = string.Join(',', tagSet.OrderBy(t => t, StringComparer.InvariantCultureIgnoreCase));
                    updatedTags.Add(item);
                }
            }

            if (updatedTags.Count > 0)
            {
                await _paymentTagRepository.UpdateManyAsync(updatedTags, autoSave: true);
            }

            return [.. updatedTags.Select(x => x.Id)];
        }

        /// <summary>
        /// Deletes a tag from all application tags. Only whole-word tags are removed; substring matches are ignored.
        /// </summary>
        /// <param name="deleteTag">String of tag to be deleted.</param>
        [Authorize(UnitySelector.SettingManagement.Tags.Delete)]
        public async Task DeleteTagAsync(string deleteTag)
        {
            Check.NotNullOrWhiteSpace(deleteTag, nameof(deleteTag));

            // Remove commas from the originalTag and replacementTag
            deleteTag = deleteTag.Replace(",", string.Empty).Trim();

            var paymentRequestTags = await _paymentTagRepository.GetListAsync(e => e.Text.Contains(deleteTag));

            var updatedTags = new List<PaymentTag>();
            var deletedTags = new List<PaymentTag>();

            foreach (var item in paymentRequestTags)
            {
                var tagSet = new HashSet<string>(
                item.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                    StringComparer.InvariantCultureIgnoreCase);

                // Only replace whole word tags - skip substring matches
                if (tagSet.Remove(deleteTag))
                {
                    if (tagSet.Count > 0)
                    {
                        item.Text = string.Join(',', tagSet.OrderBy(t => t, StringComparer.InvariantCultureIgnoreCase));
                        updatedTags.Add(item);
                    }
                    else
                    {
                        deletedTags.Add(item);
                    }
                }
            }

            if (deletedTags.Count > 0)
            {
                await _paymentTagRepository.DeleteManyAsync(deletedTags, autoSave: true);
            }

            if (updatedTags.Count > 0)
            {
                await _paymentTagRepository.UpdateManyAsync(updatedTags, autoSave: true);
            }
        }
    }
}

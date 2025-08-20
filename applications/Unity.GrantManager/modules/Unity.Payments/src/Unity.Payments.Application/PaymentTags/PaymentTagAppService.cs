using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Modules.Shared;
using Unity.Payments.Domain.PaymentTags;
using Unity.Payments.Events;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;


namespace Unity.Payments.PaymentTags
{
    [RequiresFeature("Unity.Payments")]
    [Authorize]
    public class PaymentTagAppService : PaymentsAppService, IPaymentTagAppService
    {
        private readonly IPaymentTagRepository _paymentTagRepository;
        private readonly ILocalEventBus _localEventBus;
        public PaymentTagAppService(IPaymentTagRepository paymentTagRepository, ILocalEventBus localEventBus)
        {
            _paymentTagRepository = paymentTagRepository;
            _localEventBus = localEventBus;
        }
        public async Task<IList<PaymentTagDto>> GetListAsync()
        {
            var paymentTags = await _paymentTagRepository.GetListAsync();
            return ObjectMapper.Map<List<PaymentTag>, List<PaymentTagDto>>(paymentTags.OrderBy(t => t.Id).ToList());
        }
        public async Task<IList<PaymentTagDto>> GetListWithPaymentRequestIdsAsync(List<Guid> ids)
        {
            var tagsQuery = (await _paymentTagRepository.GetQueryableAsync())
                           .Include(pt => pt.Tag)
                           .Where(e => ids.Contains(e.PaymentRequestId))
                           .OrderBy(t => t.Id);

            var tags = await tagsQuery.ToListAsync();

            return ObjectMapper.Map<List<PaymentTag>, List<PaymentTagDto>>(tags);
        }
        public async Task<PaymentTagDto?> GetPaymentTagsAsync(Guid id)
        {
            var paymentTags = await _paymentTagRepository.FirstOrDefaultAsync(s => s.PaymentRequestId == id);

            if (paymentTags == null) return null;

            return ObjectMapper.Map<PaymentTag, PaymentTagDto>(paymentTags);
        }


        public async Task<List<PaymentTagDto>> AssignTagsAsync(AssignPaymentTagDto input)
        {
            var existingApplicationTags = await _paymentTagRepository.GetListAsync(e => e.PaymentRequestId == input.PaymentRequestId);
            var existingTagIds = existingApplicationTags.Select(t => t.TagId).ToHashSet();
            var inputTagIds = input.Tags?.Select(t => t.Id).ToHashSet() ?? new HashSet<Guid>();
            var newTagsToAdd = input.Tags?
                .Where(tag => !existingTagIds.Contains(tag.Id))
                .Select(tag => new PaymentTag
                {
                    PaymentRequestId = input.PaymentRequestId,
                    TagId = tag.Id
                })
                .ToList();
            
            var tagsToRemove = existingApplicationTags
                .Where(et => !inputTagIds.Contains(et.TagId))
                .ToList();

            if (tagsToRemove.Count > 0 && await AuthorizationService.IsGrantedAsync(UnitySelector.Payment.Tags.Delete))
            {
                await _paymentTagRepository.DeleteManyAsync(tagsToRemove, autoSave: true);
            }

            if (newTagsToAdd?.Count > 0 && await AuthorizationService.IsGrantedAsync(UnitySelector.Payment.Tags.Create))
            {
                await _paymentTagRepository.InsertManyAsync(newTagsToAdd, autoSave: true);
                var tagIds = newTagsToAdd.Select(x => x.TagId).ToList();

                var insertedTagsWithNavProps = await (await _paymentTagRepository.GetQueryableAsync())
                    .Where(x => x.PaymentRequestId == input.PaymentRequestId && tagIds.Contains(x.TagId))
                    .Include(x => x.Tag)
                    .ToListAsync();

                return ObjectMapper.Map<List<PaymentTag>, List<PaymentTagDto>>(insertedTagsWithNavProps);
            }
            else
            {
                return [];
            }
        }

        [Authorize(UnitySelector.SettingManagement.Tags.Default)]
        public async Task<PagedResultDto<TagSummaryCountDto>> GetTagSummaryAsync()
        {
            var summary = await _paymentTagRepository.GetTagSummary();
            var tagSummary = ObjectMapper.Map<List<PaymentTagSummaryCount>, List<TagSummaryCountDto>>(summary
           );

            return new PagedResultDto<TagSummaryCountDto>(
                tagSummary.Count,
                tagSummary
            );
        }

        /// <summary>
        /// Deletes a tag from all application tags. Only whole-word tags are removed; substring matches are ignored.
        /// </summary>
        /// <param name="deleteTag">String of tag to be deleted.</param>
        [Authorize(UnitySelector.SettingManagement.Tags.Delete)]
        public async Task DeleteTagAsync(Guid id)
        {
            await _paymentTagRepository.DeleteAsync(id);
        }


        [Authorize(UnitySelector.SettingManagement.Tags.Delete)]
        public async Task DeleteTagWithTagIdAsync(Guid tagId)
        {
            var existingApplicationTags = await _paymentTagRepository.GetListAsync(e => e.Tag.Id == tagId);
            var idsToDelete = existingApplicationTags.Select(x => x.Id).ToList();
            await _paymentTagRepository.DeleteManyAsync(idsToDelete, autoSave: true);
            await _localEventBus.PublishAsync(new TagDeletedEto { TagId = tagId });
        }


    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GlobalTag;
using Unity.Payments.PaymentRequests;
using Unity.Payments.PaymentTags;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;


namespace Unity.Payments.Web.Pages.PaymentTags
{
    public class NewTagItem
    {
        public string? PaymentRequestId { get; set; }
        public List<GlobalTagDto> CommonTags { get; set; } = new();
        public List<GlobalTagDto> UncommonTags { get; set; } = new();
    }
    public class PaymentTagsSelectionModalModel : AbpPageModel
    {
        [BindProperty]
        [DisplayName("Tags")]
        public string? SelectedTags { get; set; } = string.Empty;

        [BindProperty]
        [DisplayName("All Tags")]
        public List<GlobalTagDto> AllTags { get; set; } = new();

        [BindProperty]
        [DisplayName("Selected Payments")]
        public string? SelectedPaymentRequestIds { get; set; } = string.Empty;

        [BindProperty]
        [DisplayName("Action Type")]
        public string? ActionType { get; set; } = string.Empty;

        private readonly IPaymentTagAppService _paymentTagsService;
        private readonly PaymentIdsCacheService _cacheService;

        [BindProperty]
        [DisplayName("Common Tags")]
        public List<GlobalTagDto> CommonTags { get; set; } = new();

        [BindProperty]
        [DisplayName("Uncommon Tags")]
        public List<GlobalTagDto> UncommonTags { get; set; } = new();

        [BindProperty]
        [DisplayName("Tags")]
        public List<NewTagItem> Tags { get; set; } = new();

        [BindProperty]
        public string? SelectedTagsJson { get; set; }

        [BindProperty]
        public string? TagsJson { get; set; }

        [BindProperty]
        [DisplayName("Cache Key")]
        public string? CacheKey { get; set; }

        public PaymentTagsSelectionModalModel(
            IPaymentTagAppService paymentTagAppService,
            PaymentIdsCacheService cacheService)
        {
            _paymentTagsService = paymentTagAppService ?? throw new ArgumentNullException(nameof(paymentTagAppService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        public async Task OnGetAsync(string cacheKey, string actionType)
        {
            ActionType = actionType;
            CacheKey = cacheKey;

            try
            {
                // Retrieve payment IDs from distributed cache
                var paymentIds = await _cacheService.GetPaymentIdsAsync(cacheKey);

                if (paymentIds == null || paymentIds.Count == 0)
                {
                    Logger.LogWarning("Cache key expired or invalid: {CacheKey}", cacheKey);
                    ViewData["Error"] = "The session has expired. Please select payments and try again.";
                    return;
                }

                // Convert to JSON string for compatibility with existing code
                SelectedPaymentRequestIds = JsonConvert.SerializeObject(paymentIds);

                // Note: Cache is NOT removed here because JavaScript needs it for tag retrieval
                // Cache will expire automatically after 10 minutes

                Logger.LogInformation("Successfully loaded payment tags modal for {Count} payments", paymentIds.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading payment tags modal");
                ViewData["Error"] = "An error occurred while loading the tags selection. Please try again.";
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            const string uncommonTags = "Uncommon Tags";
            if (SelectedPaymentRequestIds == null) return NoContent();

            try
            {
                var paymentRequestIds = JsonConvert.DeserializeObject<List<Guid>>(SelectedPaymentRequestIds);
                if (SelectedTags != null)
                {

                    var selectedTagList = DeserializeJson<List<TagDto>>(SelectedTags) ?? [];
                    if (null != paymentRequestIds)
                    {
                        var selectedPaymentRequestIds = paymentRequestIds.ToArray();

                        if (TagsJson != null)
                        {
                            var tags = JsonConvert.DeserializeObject<NewTagItem[]>(TagsJson)?.ToList();

                            await ProcessTagsAsync(uncommonTags, selectedTagList, selectedPaymentRequestIds, tags);
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, message: "Error updating payment tags");
            }

            return NoContent();
        }

        private async Task ProcessTagsAsync(string uncommonTagsLabel, List<TagDto> selectedTags, Guid[] selectedPaymentRequestIds, List<NewTagItem>? tags)
        {
            for (int i = 0; i < selectedPaymentRequestIds.Length; i++)
            {
                var item = selectedPaymentRequestIds[i];

                var tagList = new List<GlobalTagDto>();
                if (tags != null
                    && tags.Count > 0
                    && selectedTags != null
                    && selectedTags.Count > 0
                    && selectedTags.Any(t => t.Name == uncommonTagsLabel))
                {
                    NewTagItem? paymentTag = tags.Find(tagItem => tagItem.PaymentRequestId == item.ToString());

                    if (paymentTag?.UncommonTags != null)
                    {

                        tagList.AddRange(paymentTag.UncommonTags);
                    }
                }
                if (selectedTags != null && selectedTags.Count > 0)
                {
                    var commonTagsOnly = selectedTags
                   .Where(tag => tag.Name != uncommonTagsLabel).Select(tag => new GlobalTagDto
                   {
                       Id = tag.Id,
                       Name = tag.Name
                   }).ToList();
                    tagList.AddRange(commonTagsOnly);
                }
                var distinctTags = tagList
                    .Where(tag => tag != null && tag.Id != Guid.Empty)
                    .GroupBy(tag => tag.Id)
                    .Select(group => group.First())
                    .ToList();

                await _paymentTagsService.AssignTagsAsync(new AssignPaymentTagDto
                {
                    PaymentRequestId = item,
                    Tags = distinctTags
                });
            }
        }



        private static T? DeserializeJson<T>(string jsonString) where T : class
        {
            return string.IsNullOrEmpty(jsonString) ? null : JsonConvert.DeserializeObject<T>(jsonString);
        }
    }
}

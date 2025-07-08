using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Unity.GrantManager.GlobalTag;
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
        private readonly ITagsService _tagsService;


        [BindProperty]
        [DisplayName("Common Tags")]
        public List<GlobalTagDto> CommonTags { get; set; } = new();

        [BindProperty]
        [DisplayName("Uncommon Tags")]
        public List<GlobalTagDto> UncommonTags { get; set; } = new();

        [BindProperty]
        [DisplayName("Tags")]
        public List<NewTagItem> Tags { get; set; } = new();//

        [BindProperty]
        public string? SelectedTagsJson { get; set; } // receives raw JSON string

        [BindProperty]
        public string? TagsJson { get; set; }


        public PaymentTagsSelectionModalModel(IPaymentTagAppService paymentTagAppService, ITagsService tagsService)
        {
            _paymentTagsService = paymentTagAppService ?? throw new ArgumentNullException(nameof(paymentTagAppService));
            _tagsService = tagsService ?? throw new ArgumentNullException(nameof(tagsService));

        }

        public async Task OnGetAsync(string paymentRequestIds, string actionType)
        {
            CommonTags = new List<GlobalTagDto>();
            UncommonTags = new List<GlobalTagDto>();
            SelectedPaymentRequestIds = paymentRequestIds;
            ActionType = actionType;
            var paymentRequests = JsonConvert.DeserializeObject<List<Guid>>(SelectedPaymentRequestIds);
            if (paymentRequests != null && paymentRequests.Count > 0)
            {
                try
                {
                    var allTags = await _tagsService.GetListAsync();

                    var tags = await _paymentTagsService.GetListWithPaymentRequestIdsAsync(paymentRequests);                   
                    var groupedTags = tags
                        .Where(x => x.Tag != null)
                        .GroupBy(x => x.PaymentRequestId)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(x => x.Tag!).DistinctBy(t => t.Id).ToList()
                        );
                    foreach (var missingId in paymentRequests.Except(groupedTags.Keys))
                    {
                        groupedTags[missingId] = new List<GlobalTagDto>();
                    }

         
                    List<GlobalTagDto> commonTags = new();

                    if (groupedTags.Values.Any())
                    {
                        commonTags = groupedTags.Values
                            .Aggregate((prev, next) => prev.IntersectBy(next.Select(t => t.Id), t => t.Id).ToList());
                    }

                    
                    Tags = groupedTags.Select(kvp =>
                    {
                        var appId = kvp.Key;
                        var tagList = kvp.Value;

                        var uncommonTags = tagList
                            .Where(tag => !commonTags.Any(ct => ct.Id == tag.Id))
                            .ToList();

                        return new NewTagItem
                        {
                            PaymentRequestId = appId.ToString(),
                            CommonTags = commonTags.OrderBy(t => t.Name).ToList(),
                            UncommonTags = uncommonTags.OrderBy(t => t.Name).ToList()
                        };
                    }).ToList();

                    if (Tags.Count > 0)
                    {

                        CommonTags = Tags
                            .SelectMany(item => item.CommonTags)
                            .GroupBy(tag => tag.Id)
                            .Select(group => group.First())
                            .OrderBy(tag => tag.Name)
                            .ToList();

                        UncommonTags = Tags
                            .SelectMany(item => item.UncommonTags)
                            .GroupBy(tag => tag.Id)
                            .Select(group => group.First())
                            .OrderBy(tag => tag.Name)
                            .ToList();
                    }

                    AllTags = allTags
                     .DistinctBy(tag => tag.Id)
                     .OrderBy(tag => tag.Name)
                     .Select(tag => new GlobalTagDto
                     {
                         Id = tag.Id,
                         Name = tag.Name
                     })
                     .ToList();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, message: "Error loading tag select list");
                }
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

                        if (Tags != null)
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

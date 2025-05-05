using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Unity.Payments.PaymentTags;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.Payments.Web.Pages.PaymentTags
{
    class NewTagItem
    {
        public string? PaymentRequestId { get; set; }
        public string? CommonText { get; set; }
        public string? UncommonText { get; set; }
    }
    public class PaymentTagsSelectionModalModel : AbpPageModel
    {
        [BindProperty]
        [DisplayName("")]
        public string? SelectedTags { get; set; } = string.Empty;

        [BindProperty]
        public string? AllTags { get; set; } = string.Empty;

        [BindProperty]
        public string? SelectedPaymentRequestIds { get; set; } = string.Empty;

        [BindProperty]
        public string? ActionType { get; set; } = string.Empty;

        private readonly IPaymentTagAppService _paymentTagsService;


        [BindProperty]
        public string? CommonTags { get; set; } = string.Empty;

        [BindProperty]
        public string? UncommonTags { get; set; } = string.Empty;

        [BindProperty]
        public string? Tags { get; set; } = string.Empty;


        public PaymentTagsSelectionModalModel(IPaymentTagAppService paymentTagAppService)
        {
            _paymentTagsService = paymentTagAppService ?? throw new ArgumentNullException(nameof(paymentTagAppService));

        }

        public async Task OnGetAsync(string paymentRequestIds, string actionType)
        {

            SelectedPaymentRequestIds = paymentRequestIds;
            ActionType = actionType;
            var paymentRequests = JsonConvert.DeserializeObject<List<Guid>>(SelectedPaymentRequestIds);
            if (paymentRequests != null && paymentRequests.Count > 0)
            {
                try
                {
                    var allTags = await _paymentTagsService.GetListAsync();

                    var tags = await _paymentTagsService.GetListWithPaymentRequestIdsAsync(paymentRequests);

                    // Add default objects for missing paymentRequestIds
                    var missingPaymenRequestIds = paymentRequests.Except(tags.Select(tag => tag.PaymentRequestId));
                    tags = tags.Concat(missingPaymenRequestIds.Select(paymentRequestId => new PaymentTagDto
                    {
                        PaymentRequestId = paymentRequestId,
                        Text = "", // You can set default values here
                        Id = Guid.NewGuid() // Assuming Id is a Guid
                    })).ToList();

                    var newArray = tags.Select(item =>
                    {
                        var textValues = item.Text.Split(',');
                        var commonText = tags
                            .SelectMany(x => x.Text.Split(','))
                            .GroupBy(text => text)
                            .Where(group => group.Count() == tags.Count)
                            .Select(group => group.Key);

                        var uncommonText = textValues.Except(commonText);

                        return new NewTagItem
                        {
                            PaymentRequestId = item.PaymentRequestId.ToString(),
                            CommonText = string.Join(",", commonText),
                            UncommonText = string.Join(",", uncommonText)
                        };
                    }).ToArray();

                    var allUniqueCommonTexts = newArray
                        .SelectMany(item => (item.CommonText?.Split(',') ?? Array.Empty<string>()))
                        .Where(text => !string.IsNullOrEmpty(text))
                        .Distinct()
                        .OrderBy(text => text);

                    var allUniqueUncommonTexts = newArray
                        .SelectMany(item => (item.UncommonText?.Split(',') ?? Array.Empty<string>()))
                        .Where(text => !string.IsNullOrEmpty(text))
                        .Distinct()
                        .OrderBy(text => text);



                    var allUniqueTexts = allTags
                                        .SelectMany(obj => obj.Text.ToString().Split(',').Select(t => t.Trim()))
                                        .Distinct();
                    var uniqueCommonTextsString = string.Join(",", allUniqueCommonTexts);
                    var uniqueUncommonTextsString = string.Join(",", allUniqueUncommonTexts);
                    var allUniqueTextsString = string.Join(",", allUniqueTexts);

                    AllTags = allUniqueTextsString;
                    CommonTags = uniqueCommonTextsString;
                    UncommonTags = uniqueUncommonTextsString;
                    Tags = JsonConvert.SerializeObject(newArray);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, message: "Error loading tag select list");
                }
            }

        }

        public async Task<IActionResult> OnPostAsync()
        {
            const string uncommonTags = "Uncommon Tags"; // Move to constants?
            if (SelectedPaymentRequestIds == null) return NoContent();

            try
            {
                var paymentRequestIds = JsonConvert.DeserializeObject<List<Guid>>(SelectedPaymentRequestIds);
                if (SelectedTags != null)
                {
                    string[]? stringArray = JsonConvert.DeserializeObject<string[]>(SelectedTags);

                    if (null != paymentRequestIds)
                    {
                        var selectedPaymentRequestIds = paymentRequestIds.ToArray();

                        if (Tags != null)
                        {
                            var tags = JsonConvert.DeserializeObject<NewTagItem[]>(Tags)?.ToList();

                            await ProcessTagsAsync(uncommonTags, stringArray, selectedPaymentRequestIds, tags);
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

        private async Task ProcessTagsAsync(string uncommonTags, string[]? stringArray, Guid[] selectedPaymentRequestIds, List<NewTagItem>? tags)
        {
            foreach (var item in selectedPaymentRequestIds)
            {
                var paymentTagString = "";

                if (tags != null
                    && tags.Count > 0
                    && stringArray != null
                    && stringArray.Length > 0
                    && stringArray.Contains(uncommonTags))
                {
                    NewTagItem? paymentTag = tags.Find(tagItem => tagItem.PaymentRequestId == item.ToString());

                    if (paymentTag != null)
                    {
                        paymentTagString += paymentTag.UncommonText;
                    }
                }
                if (stringArray != null && stringArray.Length > 0)
                {
                    var paymentCommonTagArray = stringArray.Where(item => item != uncommonTags).ToArray();
                    if (paymentCommonTagArray.Length > 0)
                    {
                        paymentTagString += (paymentTagString == "" ? string.Join(",", paymentCommonTagArray) : (',' + string.Join(",", paymentCommonTagArray)));

                    }
                }

                await _paymentTagsService.CreateorUpdateTagsAsync(item, new PaymentTagDto { PaymentRequestId = item, Text = RemoveDuplicates(paymentTagString) });
            }
        }

        private static string RemoveDuplicates(string paymentTagString)
        {
            var tagArray = paymentTagString.Split(",");
            var noDuplicates = tagArray.Distinct().ToArray();
            return string.Join(",", noDuplicates);
        }
    }
}

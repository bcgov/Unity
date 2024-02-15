using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Unity.GrantManager.Events;
using Unity.GrantManager.GrantApplications;


namespace Unity.GrantManager.TeamsNotifications
{
    public class TeamsNotificationService
    {
        private static string TeamsNotificationChannelWebhook = "https://bcgov.webhook.office.com/webhookb2/d7f6b10c-adf3-4dd1-ad14-8f526f197bd2@6fdb5200-3d0d-4a8a-b036-d3685e359adc/IncomingWebhook/2f6fa0633b614793b3073e087d043bc4/f366bba6-e526-4920-ad6f-ce6ae509430a";


        public static async Task PostToTeamsAsync(string activityTitle, string activitySubtitle, List<Fact> facts)
        {
            string messageCard = InitializeMessageCard(activityTitle, activitySubtitle, facts);
            await PostToTeamsChannelAsync(TeamsNotificationChannelWebhook, messageCard);
        }

        private static string InitializeMessageCard(string activityTitle, string activitySubtitle, List<Fact> facts)
        {

            dynamic messageCard = MessageCard.GetMessageCard();
            JObject jsonObj = JsonConvert.DeserializeObject<dynamic>(messageCard)!;
            jsonObj["summary"] = "Message Summary";

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            (jsonObj)["sections"].Children().FirstOrDefault()["activityTitle"] = activityTitle;
            (jsonObj)["sections"].Children().FirstOrDefault()["activitySubtitle"] = activitySubtitle;
            // Add Facts
            foreach(var fact in facts)
            {
                JObject obj = JObject.Parse(JsonConvert.SerializeObject(fact));
                (jsonObj)["sections"].Children().FirstOrDefault().Value<JArray>("facts").Add(obj);
            }
#pragma warning restore CS8602 // Possible null reference argument.
            return jsonObj.ToString(Formatting.None);
        }

        public static async Task PostChefsEventToTeamsAsync(EventSubscriptionDto eventSubscriptionDto, dynamic form, dynamic chefsFormVersion)
        {
            string eventDescription = eventSubscriptionDto.SubscriptionEvent switch
            {
                ChefsEventTypesConsts.FORM_DRAFT_PUBLISHED => "A Draft CHEFS form was published",
                ChefsEventTypesConsts.FORM_PUBLISHED => "A CHEFS form was published",
                ChefsEventTypesConsts.FORM_UN_PUBLISHED => "A CHEFS form was un-published",
                _ => "An Unknown CHEFS event " + eventSubscriptionDto.SubscriptionEvent + " was fired "
            };

            JObject formObject = JObject.Parse(form.ToString());
            var formName = formObject.SelectToken("name");

            // version
            JToken? version = ((JObject)chefsFormVersion).SelectToken("version");
            JToken? published = ((JObject)chefsFormVersion).SelectToken("published");
            JToken? createdBy = ((JObject)chefsFormVersion).SelectToken("createdBy");
            JToken? createdAt = ((JObject)chefsFormVersion).SelectToken("createdAt");
            JToken? updatedBy = ((JObject)chefsFormVersion).SelectToken("updatedBy");
            JToken? updatedAt = ((JObject)chefsFormVersion).SelectToken("updatedAt");

            string? envInfo = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            string activityTitle = eventDescription + " with an event posting to the " + envInfo + " environment";
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            string activitySubtitle = "Form Name: " + formName.ToString();

            List<Fact> facts =
            [
                new Fact
                {
                    name = "Form Version: ",
                    value = version.ToString()
                },
                new Fact
                {
                    name = "Published: ",
                    value = published.ToString()
                },
                new Fact
                {
                    name = "Updated By: ",
                    value = updatedBy.ToString()
                },
                new Fact
                {
                    name = "Updated At: ",
                    value = updatedAt.ToString() + " UTC"
                },
                new Fact
                {
                    name = "Created By: ",
                    value = createdBy.ToString()
                },
                new Fact
                {
                    name = "Created At: ",
                    value = createdAt.ToString() + " UTC"
                },
            ];
#pragma warning restore CS8602

            await PostToTeamsAsync(activityTitle, activitySubtitle, facts);
        }

        private static async Task PostToTeamsChannelAsync(string teamsChannel, string messageCard) {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), teamsChannel))
                {
                    request.Content = new StringContent(messageCard);
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                    var response = await httpClient.SendAsync(request);
                }
            }
        }
    }
  
}

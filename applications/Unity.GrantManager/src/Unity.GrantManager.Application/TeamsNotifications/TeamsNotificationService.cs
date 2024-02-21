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
        public TeamsNotificationService() : base() { }

        public static async Task PostToTeamsAsync(string teamsChannel, string activityTitle, string activitySubtitle, List<Fact> facts)
        {
            if(!teamsChannel.IsNullOrEmpty()) {
                string messageCard = InitializeMessageCard(activityTitle, activitySubtitle, facts);
                await PostToTeamsChannelAsync(teamsChannel, messageCard);
            }
        }

        private static string InitializeMessageCard(string activityTitle, string activitySubtitle, List<Fact> facts)
        {
            dynamic messageCard = MessageCard.GetMessageCard();
            JObject jsonObj = JsonConvert.DeserializeObject<dynamic>(messageCard)!;
            string messageCardString = string.Empty;

            if(jsonObj != null)
            {
                jsonObj["summary"] = "Message Summary";

#pragma warning disable CS8602 // Dereference of a possibly null reference.
                if(jsonObj["sections"] != null)
                {
                    var sections = jsonObj["sections"];
                    var firstChild = sections.Children().First();
                    firstChild["activityTitle"] = activityTitle;
                    firstChild["activitySubtitle"] = activitySubtitle;
                    // Add Facts
                    foreach (var fact in facts)
                    {
                        JObject obj = JObject.Parse(JsonConvert.SerializeObject(fact));
                        firstChild.Value<JArray>("facts").Add(obj);
                    }
                }
#pragma warning restore CS8602 // Possible null reference argument
                messageCardString = jsonObj.ToString(Formatting.None);
            }

            return messageCardString;
        }

        public static async Task PostChefsEventToTeamsAsync(string teamsChannel, EventSubscriptionDto eventSubscriptionDto, dynamic form, dynamic chefsFormVersion)
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

            await PostToTeamsAsync(teamsChannel, activityTitle, activitySubtitle, facts);
        }

        private static async Task PostToTeamsChannelAsync(string teamsChannel, string messageCard) {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), teamsChannel))
                {
                    request.Content = new StringContent(messageCard);
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                    await httpClient.SendAsync(request);
                }
            }
        }
    }
  
}

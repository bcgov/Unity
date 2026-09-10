using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Unity.GrantManager.Notifications.Logs;


namespace Unity.Notifications.Teams
{
    public class LogNotificationService
    {
        public LogNotificationService() : base() { }

        public const string DIRECT_MESSAGE_KEY_PREFIX = "DIRECT_MESSAGE_";
        public const string TEAMS_ALERT = $"{DIRECT_MESSAGE_KEY_PREFIX}0";
        public const string TEAMS_NOTIFICATION = $"{DIRECT_MESSAGE_KEY_PREFIX}1";

        

        private readonly List<Fact> _facts = [];

        public async Task LogFactsToNotificationsAsync(NotificationType NotificationType, string activityTitle, string activitySubtitle)
        {

            string messageCard = InitializeMessageCard(activityTitle, activitySubtitle, _facts);
            await PostToNotificationsChannelAsync(NotificationType, messageCard);
        }

        public static async Task PostToNotificationsAsync(NotificationType NotificationType, string activityTitle, string activitySubtitle, List<Fact> facts)
        {
            string messageCard = InitializeMessageCard(activityTitle, activitySubtitle, facts);
            await PostToNotificationsChannelAsync(NotificationType, messageCard);

        }

        public List<Fact> AddFact(string Name, string Value)
        {
            var fact = new Fact
            {
                Name = Name,
                Value = Value
            };

            _facts.Add(fact);
            return _facts;
        }

        private static class ChefsEventTypesConsts
        {
            public const string FORM_PUBLISHED = "eventFormPublished";
            public const string FORM_UN_PUBLISHED = "eventFormUnPublished";
            public const string FORM_DRAFT_PUBLISHED = "eventFormDraftPublished";
        }

        public static string InitializeMessageCard(string activityTitle, string activitySubtitle, List<Fact> facts)
        {
            dynamic messageCard = MessageCard.GetMessageCard();
            JObject jsonObj = JsonConvert.DeserializeObject<dynamic>(messageCard)!;
            string messageCardString = string.Empty;

            if(jsonObj != null)
            {
                jsonObj["summary"] = "Message Summary";

                if(jsonObj["sections"] != null)
                {
                    var sections = jsonObj["sections"];
                    var firstChild = sections?.Children().First();

                    if (firstChild != null)
                    {
                        firstChild["activityTitle"] = activityTitle;
                        firstChild["activitySubtitle"] = activitySubtitle;
                        // Add Facts
                        foreach (var fact in facts)
                        {
                            JObject obj = JObject.Parse(JsonConvert.SerializeObject(fact));
                            firstChild.Value<JArray>("facts")?.Add(obj);
                        }
                    }
                }

                messageCardString = jsonObj.ToString(Formatting.None);
            }

            return messageCardString;
        }

        public static async Task PostChefsEventToNotificationsAsync(NotificationType notificationType, string subscriptionEvent, dynamic form, dynamic chefsFormVersion)
        {
            string eventDescription = subscriptionEvent switch
            {
                ChefsEventTypesConsts.FORM_DRAFT_PUBLISHED => "A Draft CHEFS form was published",
                ChefsEventTypesConsts.FORM_PUBLISHED => "A CHEFS form was published",
                ChefsEventTypesConsts.FORM_UN_PUBLISHED => "A CHEFS form was un-published",
                _ => "An Unknown CHEFS event " + subscriptionEvent + " was fired "
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

            string activitySubtitle = "Form Name: " + formName?.ToString();

            // Fix for IDE0028: Simplify collection initialization
            List<Fact> facts =
            [
                new Fact { Name = "Form Version: ", Value = version?.ToString() ?? string.Empty },
                new Fact { Name = "Published: ", Value = published?.ToString() ?? string.Empty },
                new Fact { Name = "Updated By: ", Value = updatedBy?.ToString() ?? string.Empty },
                new Fact { Name = "Updated At: ", Value = updatedAt?.ToString() + " UTC" },
                new Fact { Name = "Created By: ", Value = createdBy?.ToString() ?? string.Empty },
                new Fact { Name = "Created At: ", Value = createdAt?.ToString() + " UTC" }
            ];

            await PostToNotificationsAsync(notificationType, activityTitle, activitySubtitle, facts);
        }

        private static readonly HttpClient httpClient = new();

        /// <summary>
        /// Posts a message card to the specified Notifications channel using an HTTP POST request.
        /// </summary>
        /// <param name="notificationType">The type of notification.</param>
        /// <param name="messageCard">The message card payload in JSON format.</param>
        public static async Task PostToNotificationsChannelAsync(NotificationType notificationType, string messageCard)
        {
            // using var request = new HttpRequestMessage(HttpMethod.Post, "");
            // request.Content = new StringContent(messageCard);
            // request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            // var response = await httpClient.SendAsync(request);
            // if (!response.IsSuccessStatusCode)
            // {
            //     // Optionally log or throw an exception here
            //     throw new HttpRequestException($"Failed to post to Notifications channel. Status code: {response.StatusCode}");
            // }

            // REWRITE this  - sends to teems channel but we can't anymore
            // Would like this to create a push notification to the Unity Notifications service which will then send to Teams channel
        }
    }
}

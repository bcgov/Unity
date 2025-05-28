using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;


namespace Unity.Notifications.TeamsNotifications
{
    public class TeamsNotificationService
    {
        public TeamsNotificationService() : base() { }

        public const string DIRECT_MESSAGE_KEY_PREFIX = "DIRECT_MESSAGE_";
        public const string TEAMS_NOTIFICATION = $"{DIRECT_MESSAGE_KEY_PREFIX}0";
        public const string TEAMS_NOTIFICATION_1 = $"{DIRECT_MESSAGE_KEY_PREFIX}1";

        public static string TeamsChannel { get; set; } = string.Empty;

        private readonly List<Fact> _facts = new List<Fact>();

        public async Task PostFactsToTeamsAsync(string teamsChannel, string activityTitle, string activitySubtitle)
        {
            if (!teamsChannel.IsNullOrEmpty())
            {
                string messageCard = InitializeMessageCard(activityTitle, activitySubtitle, _facts);
                await PostToTeamsChannelAsync(teamsChannel, messageCard);
            }
        }

        public static async Task PostToTeamsAsync(string teamsChannel, string activityTitle, string activitySubtitle, List<Fact> facts)
        {
            if(!teamsChannel.IsNullOrEmpty()) {
                string messageCard = InitializeMessageCard(activityTitle, activitySubtitle, facts);
                await PostToTeamsChannelAsync(teamsChannel, messageCard);
            }
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

        public static async Task PostChefsEventToTeamsAsync(string teamsChannel, string subscriptionEvent, dynamic form, dynamic chefsFormVersion)
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

            List<Fact> facts = new()
            {
                new Fact
                {
                    Name = "Form Version: ",
                    Value = version?.ToString() ?? string.Empty
                },
                new Fact
                {
                    Name = "Published: ",
                    Value = published?.ToString() ?? string.Empty
                },
                new Fact
                {
                    Name = "Updated By: ",
                    Value = updatedBy?.ToString() ?? string.Empty
                },
                new Fact
                {
                    Name = "Updated At: ",
                    Value = updatedAt?.ToString() + " UTC"
                },
                new Fact
                {
                    Name = "Created By: ",
                    Value = createdBy?.ToString() ?? string.Empty
                },
                new Fact
                {
                    Name = "Created At: ",
                    Value = createdAt?.ToString() + " UTC"
                },
            };

            await PostToTeamsAsync(teamsChannel, activityTitle, activitySubtitle, facts);
        }

        public static async Task PostToTeamsChannelAsync(string teamsChannel, string messageCard) {
            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(new HttpMethod("POST"), teamsChannel);
            request.Content = new StringContent(messageCard);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            await httpClient.SendAsync(request);
        }
    }
}

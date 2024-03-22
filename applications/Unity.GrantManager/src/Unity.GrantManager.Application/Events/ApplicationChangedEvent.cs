using System;
using System.Text.Json.Serialization;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Events
{
    public class ApplicationChangedEvent
    {
        public Guid ApplicationId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public GrantApplicationAction Action { get; set; }
    }
}

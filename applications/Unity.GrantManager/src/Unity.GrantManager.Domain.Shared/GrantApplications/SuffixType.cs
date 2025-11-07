using System.Text.Json.Serialization;

namespace Unity.GrantManager.GrantApplications;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SuffixConfigType
{
    SequentialNumber = 1,
    SubmissionNumber = 2
}
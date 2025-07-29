using System.Text.Json.Serialization;

namespace Unity.GrantManager.GrantApplications;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AddressType
{
    PhysicalAddress = 1,
    MailingAddress = 2,
    BusinessAddress = 3
}
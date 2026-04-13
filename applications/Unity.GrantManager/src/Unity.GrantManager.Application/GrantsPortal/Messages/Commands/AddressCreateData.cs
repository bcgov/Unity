using Newtonsoft.Json;
using System;

namespace Unity.GrantManager.GrantsPortal.Messages.Commands;

public class AddressCreateData : AddressDataBase
{
    [JsonProperty("applicantId")]
    public Guid ApplicantId { get; set; }
}

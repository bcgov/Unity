using System.Collections.Generic;
using System.Collections.Immutable;

namespace Unity.GrantManager.GrantApplications;

public static class ApplicantInfoOptionsList
{
    public static ImmutableDictionary<string, string> IndigenousList =>
    ImmutableDictionary.CreateRange(new[]
      {
            new KeyValuePair<string, string>("YES", "Yes"),
            new KeyValuePair<string, string>("NO", "No"),
      });

}

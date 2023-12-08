using System.Collections.Generic;
using System.Collections.Immutable;

namespace Unity.GrantManager.GrantApplications;

public static class ProjectInfoOptionsList
{
    public static ImmutableDictionary<string, string> ForestryList =>
    ImmutableDictionary.CreateRange(new[]
      {
          new KeyValuePair<string, string>("FORESTRY", "Forestry"),
          new KeyValuePair<string, string>("NON_FORESTRY", "Non-Forestry"),
      });

    public static ImmutableDictionary<string, string> ForestryFocusList =>
    ImmutableDictionary.CreateRange(new[]
      {
          new KeyValuePair<string, string>("PRIMARY", "Primary processing"),
          new KeyValuePair<string, string>("SECONDARY", "Secondary/Value-Added/Not Mass Timber"),
          new KeyValuePair<string, string>("MASS_TIMBER", "Mass Timber"),
      });

    public static ImmutableDictionary<string, string> AcquisitionList =>
    ImmutableDictionary.CreateRange(new[]
      {
            new KeyValuePair<string, string>("YES", "Yes"),
            new KeyValuePair<string, string>("NO", "No"),
      });

}

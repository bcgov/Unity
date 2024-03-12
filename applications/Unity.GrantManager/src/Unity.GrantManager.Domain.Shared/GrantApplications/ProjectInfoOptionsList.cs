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

     public static ImmutableDictionary<string, string> OrganizationTypeList =>
     ImmutableDictionary.CreateRange(new[]
       {
              new KeyValuePair<string, string>("CORPORATION", "Corporation"),
              new KeyValuePair<string, string>("PARTNERSHIP", "Partnership"),
              new KeyValuePair<string, string>("INCORPORATED_COOPERATIVE", "Incorporated Cooperative"),
              new KeyValuePair<string, string>("FIRST_NATION", "First Nation or Indigenous-Owned Enterprises"),
              new KeyValuePair<string, string>("OTHER", "Other"),
       });

    public static ImmutableDictionary<string, string> OrgBookStatusList =>
   ImmutableDictionary.CreateRange(new[]
     {
              new KeyValuePair<string, string>("ACTIVE", "Active"),
              new KeyValuePair<string, string>("HISTORICAL", "Historical"),
     });

}

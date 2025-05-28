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

            new KeyValuePair<string, string>("BC", "BC Company"),
            new KeyValuePair<string, string>("CP", "Cooperative"),
            new KeyValuePair<string, string>("GP", "General Partnership"),
            new KeyValuePair<string, string>("S", "Society"),
            new KeyValuePair<string, string>("SP", "Sole Proprietorship"),
            new KeyValuePair<string, string>("A", "Extraprovincial Company"),
            new KeyValuePair<string, string>("B", "Extraprovincial"),
            new KeyValuePair<string, string>("BEN", "Benefit Company"),
            new KeyValuePair<string, string>("C", "Continuation In"),
            new KeyValuePair<string, string>("CC", "BC Community Contribution Company"),
            new KeyValuePair<string, string>("CS", "Continued In Society"),
            new KeyValuePair<string, string>("CUL", "Continuation In as a BC ULC"),
            new KeyValuePair<string, string>("EPR", "Extraprovincial Registration"),
            new KeyValuePair<string, string>("FI", "Financial Institution"),
            new KeyValuePair<string, string>("FOR", "Foreign Registration"),
            new KeyValuePair<string, string>("LIB", "Public Library Association"),
            new KeyValuePair<string, string>("LIC", "Licensed (Extra-Pro)"),
            new KeyValuePair<string, string>("LL", "Limited Liability Partnership"),
            new KeyValuePair<string, string>("LLC", "Limited Liability Company"),
            new KeyValuePair<string, string>("LP", "Limited Partnership"),
            new KeyValuePair<string, string>("MF", "Miscellaneous Firm"),
            new KeyValuePair<string, string>("PA", "Private Act"),
            new KeyValuePair<string, string>("PAR", "Parish"),
            new KeyValuePair<string, string>("QA", "CO 1860"),
            new KeyValuePair<string, string>("QB", "CO 1862"),
            new KeyValuePair<string, string>("QC", "CO 1878"),
            new KeyValuePair<string, string>("QD", "CO 1890"),
            new KeyValuePair<string, string>("QE", "CO 1897"),
            new KeyValuePair<string, string>("REG", "Registraton (Extra-pro)"),
            new KeyValuePair<string, string>("ULC", "BC Unlimited Liability Company"),
            new KeyValuePair<string, string>("XCP", "Extraprovincial Cooperative"),
            new KeyValuePair<string, string>("XL", "Extrapro Limited Liability Partnership"),
            new KeyValuePair<string, string>("XP", "Extraprovincial Limited Partnership"),
            new KeyValuePair<string, string>("XS", "Extraprovincial Society"),
       });

    public static ImmutableDictionary<string, string> OrgBookStatusList =>
   ImmutableDictionary.CreateRange(new[]
     {
              new KeyValuePair<string, string>("ACTIVE", "Active"),
              new KeyValuePair<string, string>("HISTORICAL", "Historical"),
     });

}

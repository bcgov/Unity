using System.Collections.Generic;
using System.Collections.Immutable;

namespace Unity.GrantManager.GrantApplications;

public static class AssessmentResultsOptionsList
{
    public static ImmutableDictionary<string, string> FundingList =>
     ImmutableDictionary.CreateRange(new[]
        {
            new KeyValuePair<string, string>("LOW", "Low"),
            new KeyValuePair<string, string>("MEDIUM", "Medium"),
            new KeyValuePair<string, string>("HIGH", "High"),
        });

    public static ImmutableDictionary<string, string> DueDilligenceList =>
    ImmutableDictionary.CreateRange(new[]
       {
            new KeyValuePair<string, string>("COMPLETE", "Complete"),
            new KeyValuePair<string, string>("UNDERWAY", "Underway"),
            new KeyValuePair<string, string>("PAUSED", "Paused"),
            new KeyValuePair<string, string>("WITHDRAWN", "Withdrawn"),
            new KeyValuePair<string, string>("INELIGIBLE", "Ineligible"),
            new KeyValuePair<string, string>("FAILED", "Failed"),
       });

    public static ImmutableDictionary<string, string> AssessmentResultStatusList =>
    ImmutableDictionary.CreateRange(new[]
      {
            new KeyValuePair<string, string>("PASS", "Pass"),
            new KeyValuePair<string, string>("FAIL", "Fail"),
            new KeyValuePair<string, string>("INELIGIBLE", "Ineligible")
      });

    public static ImmutableDictionary<string, string> DeclineRationalActionList =>
    ImmutableDictionary.CreateRange(new[]
      {
            new KeyValuePair<string, string>("NO_READINESS", "Lack of readiness"),
            new KeyValuePair<string, string>("LOW_PRIORITY", "Lower priority relative to other requests"),
            new KeyValuePair<string, string>("NOT_ENOUGH_INFO", "Insufficient information provided"),
            new KeyValuePair<string, string>("INELIGIBLE_PROJECT", "Ineligible Project"),
            new KeyValuePair<string, string>("INELIGIBLE_APPLICANT", "Ineligible Applicant"),
            new KeyValuePair<string, string>("INSUFFICIENT_READINESS", "Insufficient Readiness"),
            new KeyValuePair<string, string>("SMALL_PROJECT", "Project too small"),
            new KeyValuePair<string, string>("OTHER", "Other"),
      });

    public static ImmutableDictionary<string, string> RecommendationActionList => 
    ImmutableDictionary.CreateRange(new[]
    {
        new KeyValuePair<string, string>("APPROVE", "Recommended for Approval"),
        new KeyValuePair<string, string>("DENY", "Recommended for Denial"),
    });
}

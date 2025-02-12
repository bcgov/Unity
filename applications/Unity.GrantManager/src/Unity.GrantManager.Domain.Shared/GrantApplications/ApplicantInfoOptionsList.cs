using System.Collections.Generic;
using System.Collections.Immutable;

namespace Unity.GrantManager.GrantApplications;

public static class ApplicantInfoOptionsList
{
    public static ImmutableDictionary<string, string> IndigenousList =>
    ImmutableDictionary.CreateRange(new[]
      {
            new KeyValuePair<string, string>("Yes", "Yes"),
            new KeyValuePair<string, string>("No", "No"),
      });

    public static ImmutableDictionary<string, string> FiscalDayList =>
    ImmutableDictionary.CreateRange(new[]
    {
          new KeyValuePair<string, string>("1", "1"),
          new KeyValuePair<string, string>("2", "2"),
          new KeyValuePair<string, string>("3", "3"),
          new KeyValuePair<string, string>("4", "4"),
          new KeyValuePair<string, string>("5", "5"),
          new KeyValuePair<string, string>("6", "6"),
          new KeyValuePair<string, string>("7", "7"),
          new KeyValuePair<string, string>("8", "8"),
          new KeyValuePair<string, string>("9", "9"),
          new KeyValuePair<string, string>("10", "10"),
          new KeyValuePair<string, string>("11", "11"),
          new KeyValuePair<string, string>("12", "12"),
          new KeyValuePair<string, string>("13", "13"),
          new KeyValuePair<string, string>("14", "14"),
          new KeyValuePair<string, string>("15", "15"),
          new KeyValuePair<string, string>("16", "16"),
          new KeyValuePair<string, string>("17", "17"),
          new KeyValuePair<string, string>("18", "18"),
          new KeyValuePair<string, string>("19", "19"),
          new KeyValuePair<string, string>("20", "20"),
          new KeyValuePair<string, string>("21", "21"),
          new KeyValuePair<string, string>("22", "22"),
          new KeyValuePair<string, string>("23", "23"),
          new KeyValuePair<string, string>("24", "24"),
          new KeyValuePair<string, string>("25", "25"),
          new KeyValuePair<string, string>("26", "26"),
          new KeyValuePair<string, string>("27", "27"),
          new KeyValuePair<string, string>("28", "28"),
          new KeyValuePair<string, string>("29", "29"),
          new KeyValuePair<string, string>("30", "30"),
          new KeyValuePair<string, string>("31", "31"),
    });

    public static ImmutableDictionary<string, string> FiscalMonthList =>
    ImmutableDictionary.CreateRange(new[]
    {
          new KeyValuePair<string, string>("Jan", "January"),
          new KeyValuePair<string, string>("Feb", "February"),
          new KeyValuePair<string, string>("Mar", "March"),
          new KeyValuePair<string, string>("Apr", "April"),
          new KeyValuePair<string, string>("May", "May"),
          new KeyValuePair<string, string>("Jun", "June"),
          new KeyValuePair<string, string>("Jul", "July"),
          new KeyValuePair<string, string>("Aug", "August"),
          new KeyValuePair<string, string>("Sep", "September"),
          new KeyValuePair<string, string>("Oct", "October"),
          new KeyValuePair<string, string>("Nov", "November"),
          new KeyValuePair<string, string>("Dec", "December"),
    });

}

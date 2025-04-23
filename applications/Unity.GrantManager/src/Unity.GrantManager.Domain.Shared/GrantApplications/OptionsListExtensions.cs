using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Unity.GrantManager.GrantApplications;
public static class OptionsListExtensions
{
    public static List<SelectListItem> FormatOptionsList<T>(
        this ImmutableDictionary<string, string> optionsList)
        => optionsList
            .Select(entry => new SelectListItem { Value = entry.Key, Text = entry.Value })
            .ToList();

    public static List<SelectListItem> FormatOptionsList<T>(
        this ImmutableDictionary<string, string> optionsList,
        Func<SelectListItem, T>? orderBy = null)
        => optionsList
            .Select(entry => new SelectListItem { Value = entry.Key, Text = entry.Value })
            .OrderBy(orderBy ?? (_ => default(T)!))
            .ToList();
}

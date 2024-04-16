using System.Collections.Generic;
using Volo.Abp.Localization;

namespace Unity.AspNetCore.Mvc.UI.Theme.UX2.Themes.UX2.Components.Toolbar.LanguageSwitch;

public class LanguageSwitchViewComponentModel
{
    public LanguageInfo CurrentLanguage { get; set; }

    public List<LanguageInfo> OtherLanguages { get; set; }
}

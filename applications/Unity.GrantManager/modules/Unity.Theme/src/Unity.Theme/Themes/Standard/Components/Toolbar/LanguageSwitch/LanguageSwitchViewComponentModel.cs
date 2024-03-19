using System.Collections.Generic;
using Volo.Abp.Localization;

namespace Unity.AspNetCore.Mvc.UI.Themes.Themes.Standard.Components.Toolbar.LanguageSwitch;

public class LanguageSwitchViewComponentModel
{
    public LanguageInfo CurrentLanguage { get; set; }

    public List<LanguageInfo> OtherLanguages { get; set; }
}

namespace Unity.AspNetCore.Mvc.UI.Theme.UX2.Bundling;

public static class UnityThemeUX2Bundles
{
    public static class Styles
    {
        public const string Global = "UX2.Global";
    }

    public static class Scripts
    {
        public const string Global = "UX2.Global";
    }
}

/*
 * This file is duplicated so that the bundles can be referenced
 * we should move the Application.cshtml file to main web app so that modules can reference themes
 * without the need for the theme to reference to modules back
*/

public static class NotificationsThemeBundles
{
    public static class Styles
    {
        public const string Notifications = "Notifications";
        //This name needs to match the one in web Moduel to resolve because of this monkey patch
    }
}

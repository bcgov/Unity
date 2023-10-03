using System;

namespace Unity.GrantManager.Web.Views.Shared
{
    public static class UserInfoExtensions
    {
        public static string GetUserBadge(this string displayName)
        {
            if (displayName == null) return string.Empty;

            string[] nameSplit = displayName.Split(new string[] { ",", " " }, StringSplitOptions.RemoveEmptyEntries);

            string initials = "";

            foreach (string item in nameSplit)
            {
                initials += item[..1].ToUpper(System.Globalization.CultureInfo.CurrentCulture);
            }

            return initials;
        }
    }
}

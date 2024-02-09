using Volo.Abp.Identity;

namespace Unity.GrantManager.Identity
{
    public static class Utils
    {
        public static string CreateUserBadge(IdentityUser identityUser)
        {
            var chars = new char?[2];
            chars[0] = identityUser.Name?.Length > 0 ? identityUser.Name?[0] : null;
            chars[1] = identityUser.Surname?.Length > 0 ? identityUser.Surname?[0] : null;
            return $"{chars[1]}{chars[0]}";
        }

        public static string CreateUserBadge(string displayName)
        {
            var names = displayName.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            var chars = new char?[2];
            chars[0] = names.Length > 0 ? names[0][0] : null;
            chars[1] = names.Length > 1 ? names[1][0] : null;
            return $"{chars[0]}{chars[1]}";
        }
    }
}

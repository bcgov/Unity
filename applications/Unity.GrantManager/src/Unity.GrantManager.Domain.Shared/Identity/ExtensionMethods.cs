namespace Unity.GrantManager.Identity
{
    public static class ExtensionMethods
    {
        public static string ToSubjectWithoutIdp(this string subject)
        {
            var idpSplitter = subject.IndexOf('@');
            var userIdentifier = (idpSplitter == -1 ? subject : subject[..idpSplitter]).ToUpper();
            return userIdentifier;
        }
    }
}

namespace Unity.GrantManager.Identity
{
    public static class ExtensionMethods
    {
        public static string ToSubjectWithoutIdp(this string subject)
        {
            var idpSplitter = subject.IndexOf("@");
            var userIdentifier = subject.Substring(0, (idpSplitter == -1 ? subject.Length : idpSplitter)).ToUpper();
            return userIdentifier;
        }
    }
}

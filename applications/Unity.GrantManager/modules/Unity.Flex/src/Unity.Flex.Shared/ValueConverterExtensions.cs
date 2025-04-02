namespace Unity.Flex
{
    public static class ValueConverterExtensions
    {
        public static bool IsTruthy(this string? value)
        {
            if (value == null
                || value.Equals("true", System.StringComparison.CurrentCultureIgnoreCase)
                || value.Equals("1", System.StringComparison.CurrentCultureIgnoreCase)
                || value.Equals("on", System.StringComparison.CurrentCultureIgnoreCase)
                || value.StartsWith("true", System.StringComparison.CurrentCultureIgnoreCase)
                || value.StartsWith("yes", System.StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }
}

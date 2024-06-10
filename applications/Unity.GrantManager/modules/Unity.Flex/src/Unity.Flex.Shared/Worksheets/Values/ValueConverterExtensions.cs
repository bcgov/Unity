namespace Unity.Flex.Worksheets.Values
{
    public static class ValueConverterExtensions
    {
        public static bool IsTruthy(this string? value)
        {
            if (value == null 
                || value.Equals("true", System.StringComparison.CurrentCultureIgnoreCase) 
                || value.Equals("1", System.StringComparison.CurrentCultureIgnoreCase) 
                || value.Equals("on", System.StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }
}

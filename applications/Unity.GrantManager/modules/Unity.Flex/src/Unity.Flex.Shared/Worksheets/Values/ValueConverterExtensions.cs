namespace Unity.Flex.Worksheets.Values
{
    public static class ValueConverterExtensions
    {
        public static bool IsTruthy(this string? value)
        {
            if (value == null || value.ToLower() == "true" || value == "1" || value == "on")
            {
                return true;
            }

            return false;
        }
    }
}

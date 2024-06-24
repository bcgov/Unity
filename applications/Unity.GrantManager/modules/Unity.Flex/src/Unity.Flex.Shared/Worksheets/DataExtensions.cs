namespace Unity.Flex.Worksheets
{
    public static class DataExtensions
    {
        public static string SanitizeWorksheetName(this string name)
        {
            return name.Trim().Replace(" ", "").ToLower();
        }
    }
}

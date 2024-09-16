namespace Unity.Flex.Domain.Utils
{
    public static class SheetParserFunctions
    {
        public static string[] SplitSheetNameAndVersion(string name)
        {
            var versionIndicator = name.LastIndexOf('-');
            return [name[0..versionIndicator], name[versionIndicator..]];
        }
    }
}

using Unity.Flex.Worksheets.Definitions;

namespace Unity.Flex.Worksheets
{
    public class CurrencyDefinition : DefinitionBase
    {
        public CurrencyDefinition()
        {
            Min = 0;
            Max = 999999999999;
            MinLength = null;
            MaxLength = null;
        }
    }
}

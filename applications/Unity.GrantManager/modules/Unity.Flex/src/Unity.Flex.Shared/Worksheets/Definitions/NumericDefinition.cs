using Unity.Flex.Worksheets.Definitions;

namespace Unity.Flex.Worksheets
{
    public class NumericDefinition : DefinitionBase
    {
        public NumericDefinition()
        {
            Min = 0;
            Max = 999999999999;
            MinLength = null;
            MaxLength = null;
        }
    }
}

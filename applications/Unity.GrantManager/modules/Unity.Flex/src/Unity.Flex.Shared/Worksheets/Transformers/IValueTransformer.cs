using Newtonsoft.Json.Linq;
using Unity.Flex.Worksheets.Definitions;
using Unity.Flex.Worksheets.Values;

namespace Unity.Flex.Worksheets.Transformers
{
    public interface IValueTransformer<in Input, out Output> 
        where Input : JToken
        where Output: CustomValueBase        
    {
        Output Transform(Input value);
    }
}

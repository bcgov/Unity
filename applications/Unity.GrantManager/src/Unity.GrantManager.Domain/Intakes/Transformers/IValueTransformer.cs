using Newtonsoft.Json.Linq;
using Unity.Flex.Worksheets.Values;

namespace Unity.GrantManager.Intakes
{
    public interface IValueTransformer<in Input, out Output> 
        where Input : JToken
        where Output: CustomValueBase        
    {
        Output Transform(Input value);
    }
}

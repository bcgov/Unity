using System.Threading.Tasks;
using Unity.Flex.Worksheets.Values;

namespace Unity.Flex.Worksheets.Collectors
{
    public interface IDataCollector<in Input, Output>
       where Input : CustomValueBase
       where Output : CustomValueBase
    {
        Task<Output> CollectAsync(Input value);
    }
}

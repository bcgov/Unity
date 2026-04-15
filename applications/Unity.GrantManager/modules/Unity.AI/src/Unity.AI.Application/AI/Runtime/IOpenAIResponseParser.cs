using System.Collections.Generic;
using Unity.AI.Responses;

namespace Unity.AI.Runtime;

public interface IOpenAIResponseParser
{
    ApplicationAnalysisResponse ParseApplicationAnalysisResponse(string raw);
    ApplicationScoringResponse ParseApplicationScoringResponse(string raw, IReadOnlyDictionary<string, string>? questionIdAliasMap = null);
}

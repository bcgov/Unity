using System.Collections.Generic;

namespace Unity.AI.Runtime;

internal interface IOpenAIPromptRenderer
{
    string ResolvePromptVersion(string? version);

    string BuildApplicationAnalysisSystemPrompt(string version);

    string BuildApplicationAnalysisUserPrompt(string version, string schema, string data, string attachments);

    string BuildAttachmentSummarySystemPrompt(string version);

    string BuildAttachmentSummaryUserPrompt(string version, string attachment);

    string BuildApplicationScoringSystemPrompt(string version);

    string BuildApplicationScoringUserPrompt(string version, string data, string attachments, string section, string response);

    string BuildApplicationScoringResponseTemplate(string sectionPayloadJson);

    string BuildAliasedApplicationScoringSection(string? sectionName, string sectionJson, out IReadOnlyDictionary<string, string> questionIdAliasMap);
}

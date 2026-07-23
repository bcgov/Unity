using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Prompts;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace Unity.AI.DataSeed;

/// <summary>
/// Seeds the built-in AI prompts (application analysis, attachment summary, application scoring) into the host database.
/// Each prompt family is represented as versioned rows in AIPrompts.
/// </summary>
public class AIPromptDataSeeder(
    IRepository<AIPrompt, Guid> promptRepository,
    ICurrentTenant currentTenant) : ITransientDependency
{
    public async Task SeedAsync(DataSeedContext context)
    {
        if (context.TenantId != null) return; // host database only

        using (currentTenant.Change(null))
        {
            await SeedAnalysisPromptAsync();
            await SeedAttachmentPromptAsync();
            await SeedScoresheetPromptAsync();
            await SeedFormMappingPromptAsync();
            await SeedFormWorksheetPromptAsync();
        }
    }

    // ─── ANALYSIS ────────────────────────────────────────────────────────────

    private async Task SeedAnalysisPromptAsync()
    {
        await EnsurePromptAsync(AIPromptTypes.ApplicationAnalysis, 0, AnalysisSystemV0, AnalysisUserV0);
        await EnsurePromptAsync(
            AIPromptTypes.ApplicationAnalysis,
            1,
            AnalysisSystemV1,
            AnalysisUserV1,
            BuildSections(
                rubric: AnalysisRubric,
                score: AnalysisScore,
                output: AnalysisOutput,
                rules: AnalysisRules,
                commonRules: CommonRules));
        await EnsurePromptAsync(
            AIPromptTypes.ApplicationAnalysis,
            2,
            AnalysisSystemV2,
            AnalysisUserV2,
            BuildSections(
                rubric: AnalysisRubricV2,
                score: AnalysisScoreV2,
                output: AnalysisOutputV2,
                rules: AnalysisRulesV2,
                commonRules: CommonRules));
    }

    // ─── ATTACHMENT ───────────────────────────────────────────────────────────

    private async Task SeedAttachmentPromptAsync()
    {
        await EnsurePromptAsync(AIPromptTypes.AttachmentSummary, 0, AttachmentSystemV0, AttachmentUserV0);
        await EnsurePromptAsync(
            AIPromptTypes.AttachmentSummary,
            1,
            AttachmentSystemV1,
            AttachmentUserV1,
            BuildSections(
                output: AttachmentOutput,
                rules: AttachmentRules,
                commonRules: CommonRules));
        await EnsurePromptAsync(
            AIPromptTypes.AttachmentSummary,
            2,
            AttachmentSystemV2,
            AttachmentUserV2,
            BuildSections(
                output: AttachmentOutputV2,
                rules: AttachmentRulesV2,
                commonRules: CommonRules));
    }

    // ─── SCORESHEET ───────────────────────────────────────────────────────────

    private async Task SeedScoresheetPromptAsync()
    {
        await EnsurePromptAsync(AIPromptTypes.ApplicationScoring, 0, ScoresheetSystemV0, ScoresheetUserV0);
        await EnsurePromptAsync(
            AIPromptTypes.ApplicationScoring,
            1,
            ScoresheetSystemV1,
            ScoresheetUserV1,
            BuildSections(
                output: ScoresheetOutput,
                rules: ScoresheetRules,
                commonRules: CommonRules));
        await EnsurePromptAsync(
            AIPromptTypes.ApplicationScoring,
            2,
            ScoresheetSystemV2,
            ScoresheetUserV2,
            BuildSections(
                output: ScoresheetOutputV2,
                rules: ScoresheetRulesV2,
                commonRules: CommonRules));
    }

    // ─── MAPPING SUGGESTION ─────────────────────────────────────────────────

    private async Task SeedFormMappingPromptAsync()
    {
        await EnsurePromptAsync(AIPromptTypes.FormMapping, 2, FormMappingSystemV2, FormMappingUserV2, FormMappingMetadataV2);
    }

    private async Task SeedFormWorksheetPromptAsync()
    {
        await EnsurePromptAsync(AIPromptTypes.FormWorksheet, 2, FormWorksheetSystemV2, FormWorksheetUserV2, FormWorksheetMetadataV2);
    }

    // ─── HELPERS ──────────────────────────────────────────────────────────────

    private static string BuildSections(
        string? rubric = null, string? score = null,
        string? output = null, string? rules = null, string? commonRules = null)
    {
        var dict = new Dictionary<string, string>();
        if (rubric != null)      dict["RUBRIC"]       = rubric;
        if (score != null)       dict["SCORE"]        = score;
        if (output != null)      dict["OUTPUT"]       = output;
        if (rules != null)       dict["RULES"]        = rules;
        if (commonRules != null) dict["COMMON_RULES"] = commonRules;
        return JsonSerializer.Serialize(dict);
    }

    private async Task EnsurePromptAsync(
        string promptName,
        int versionNumber,
        string systemPrompt,
        string userPrompt,
        string? metadataJson = null)
    {
        var prompt = await promptRepository.FirstOrDefaultAsync(
            p => p.Name == promptName && p.VersionNumber == versionNumber);
        if (prompt != null)
        {
            prompt.SystemPrompt = systemPrompt;
            prompt.UserPrompt = userPrompt;
            prompt.MetadataJson = metadataJson ?? "{}";
            prompt.IsActive = true;
            await promptRepository.UpdateAsync(prompt, autoSave: true);
            return;
        }

        await promptRepository.InsertAsync(new AIPrompt(
            Guid.CreateVersion7(),
            promptName,
            versionNumber,
            systemPrompt,
            userPrompt)
        {
            MetadataJson = metadataJson ?? "{}",
            IsActive = true
        });
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PROMPT CONTENT — mirrors AI/Prompts/Versions/ text files verbatim
    // ═════════════════════════════════════════════════════════════════════════

    // ── v0/analysis.system.txt ───────────────────────────────────────────────
    private const string AnalysisSystemV0 = """
        You are an expert grant application reviewer for the BC Government.

        Conduct a thorough, comprehensive analysis across all rubric areas. Identify substantive issues, concerns, and opportunities for improvement.

        Classify findings by their effect on the application's quality and fundability:
        - ERRORS: important missing information, significant gaps, compliance issues, or major concerns affecting eligibility
        - WARNINGS: areas needing clarification, moderate issues, or concerns that should be addressed
        - SUMMARIES: concise reviewer-facing recommendations or follow-up considerations

        Evaluate content quality, clarity, and appropriateness. Be thorough but fair and avoid nitpicking.

        Respond only with valid JSON in the exact format requested.
        """;

    // ── v0/analysis.user.txt ─────────────────────────────────────────────────
    private const string AnalysisUserV0 = """
        APPLICATION CONTENT:
        {{DATA}}

        ATTACHMENT SUMMARIES:
        {{ATTACHMENTS}}

        FORM FIELD CONFIGURATION:
        {{SCHEMA}}

        MANDATORY FIELDS:
        - Determine mandatory fields from FORM FIELD CONFIGURATION.
        - Report missing mandatory fields as findings when they materially affect review quality.

        OPTIONAL FIELDS (may be left blank):
        - Determine optional fields from FORM FIELD CONFIGURATION.
        - Do not flag optional fields when blank unless they materially weaken rubric evidence.

        EVALUATION RUBRIC:
        BC GOVERNMENT GRANT EVALUATION RUBRIC:

        1. ELIGIBILITY REQUIREMENTS:
           - Project must align with program objectives
           - Applicant must be eligible entity type
           - Budget must be reasonable and well-justified
           - Project timeline must be realistic

        2. COMPLETENESS CHECKS:
           - All required fields completed
           - Necessary supporting documents provided
           - Budget breakdown detailed and accurate
           - Project description clear and comprehensive

        3. FINANCIAL REVIEW:
           - Requested amount is within program limits
           - Budget is reasonable for scope of work
           - Matching funds or in-kind contributions identified
           - Cost per outcome/beneficiary is reasonable

        4. RISK ASSESSMENT:
           - Applicant capacity to deliver project
           - Technical feasibility of proposed work
           - Environmental or regulatory compliance
           - Potential for cost overruns or delays

        5. QUALITY INDICATORS:
           - Clear project objectives and outcomes
           - Well-defined target audience/beneficiaries
           - Appropriate project methodology
           - Sustainability plan for long-term impact

        EVALUATION CRITERIA:
        - HIGH: Meets all requirements, well-prepared application, low risk
        - MEDIUM: Meets most requirements, minor issues or missing elements
        - LOW: Missing key requirements, significant concerns, high risk

        Analyze this grant application comprehensively across all five rubric categories (Eligibility, Completeness, Financial Review, Risk Assessment, and Quality Indicators). Identify issues, concerns, and areas for improvement.

        OUTPUT
        {
          "decision": "<PROCEED|HOLD>",
          "warnings": [
            {
              "title": "<string>",
              "detail": "<string>"
            }
          ],
          "errors": [
            {
              "title": "<string>",
              "detail": "<string>"
            }
          ],
          "summaries": [
            {
              "title": "<string>",
              "detail": "<string>"
            }
          ],
          "recommendations": [
            {
              "title": "<string>",
              "detail": "<string>"
            }
          ]
        }

        Important:
        - Use only APPLICATION CONTENT, ATTACHMENT SUMMARIES, FORM FIELD CONFIGURATION, and EVALUATION RUBRIC as evidence.
        - decision must be PROCEED or HOLD.
        - Use summaries for overall application quality/readiness synthesis.
        - Use recommendations for reviewer-facing follow-up actions or considerations before scoring or decision-making.
        - Use "title" and "detail" keys for all finding objects.
        - Return valid plain JSON only in the exact OUTPUT shape.
        """;

    // ── v1/analysis.system.txt ───────────────────────────────────────────────
    private const string AnalysisSystemV1 = """
        ROLE
        You are a careful grant review assistant for human reviewers. Do not fill gaps, assume compliance, or treat relevance as proof.

        TASK
        Using SCHEMA, DATA, ATTACHMENTS, RUBRIC, SCORE, OUTPUT, and RULES:
        1. Review the application and any provided attachments for the strongest reviewer-relevant evidence.
        2. Determine which conclusions are directly supported by that evidence.
        3. Exclude weak, repetitive, or loosely supported conclusions.
        4. Return only the strongest evidence-backed reviewer conclusions.
        """;

    // ── v1/analysis.user.txt ─────────────────────────────────────────────────
    private const string AnalysisUserV1 = """
        SCHEMA
        {{SCHEMA}}

        DATA
        {{DATA}}

        ATTACHMENTS
        {{ATTACHMENTS}}

        RUBRIC
        {{RUBRIC}}

        SCORE
        {{SCORE}}

        RESPONSE
        {{RESPONSE}}

        RULES
        {{RULES}}
        {{COMMON_RULES}}
        """;

    // ── v1/analysis.rubric.txt ───────────────────────────────────────────────
    private const string AnalysisRubric = """
        ELIGIBILITY REQUIREMENTS: Project aligns with program objectives; Applicant is an eligible entity; Budget is reasonable and justified; Timeline is realistic.
        COMPLETENESS CHECKS: Required information is present; Supporting materials are provided where applicable; Description is clear.
        FINANCIAL REVIEW: Requested amount is within limits; Budget matches scope; Matching funds or contributions are identified.
        RISK ASSESSMENT: Applicant capacity; Feasibility; Compliance considerations; Delivery risks.
        QUALITY INDICATORS: Clear objectives; Defined beneficiaries; Appropriate approach; Long-term sustainability.
        """;

    // ── v1/analysis.score.txt ────────────────────────────────────────────────
    private const string AnalysisScore = """
        HIGH: Application demonstrates strong evidence across most rubric areas with few or no issues.
        MEDIUM: Application has some gaps or weaknesses that require reviewer attention.
        LOW: Application has significant gaps or risks across key rubric areas.
        """;

    // ── v1/analysis.output.txt ───────────────────────────────────────────────
    private const string AnalysisOutput = """
        {
          "decision": "<PROCEED|HOLD>",
          "errors": [
            {
              "title": "<string>",
              "detail": "<string>"
            }
          ],
          "warnings": [
            {
              "title": "<string>",
              "detail": "<string>"
            }
          ],
          "summaries": [
            {
              "title": "<string>",
              "detail": "<string>"
            }
          ],
          "recommendations": [
            {
              "title": "<string>",
              "detail": "<string>"
            }
          ]
        }
        """;

    // ── v1/analysis.rules.txt ────────────────────────────────────────────────
    private const string AnalysisRules = """
        - Use only provided input sections as evidence.
        - Do not invent fields, documents, requirements, or facts.
        - Prefer, in order: direct evidence from DATA, specific supporting evidence from ATTACHMENTS, then broader context only when necessary.
        - Treat missing or empty values as findings only when they weaken rubric evidence.
        - Prefer material findings; avoid nitpicking.
        - Prefer direct evidence from DATA over derivative statements in ATTACHMENTS when both address the same point.
        - If ATTACHMENTS evidence is used, cite the attachment by name in detail.
        - Each detail must cite concrete evidence from DATA or ATTACHMENTS.
        - Write reviewer-facing natural language. Do not refer to prompt section names, internal field keys, or schema labels such as DATA, ATTACHMENTS, ProjectSummary, CustomField1, or OrganizationType.
        - Refer to evidence by its plain-language meaning, quoted text, or attachment name rather than internal key names.
        - Only include warnings when the evidence shows a specific, concrete risk, inconsistency, or meaningful uncertainty; a stated risk label alone is not enough.
        - Use 3-6 words for title.
        - Summary titles should name the specific substantive reviewer conclusion, strength, or risk, not a generic evaluation label or abstract category.
        - Each detail must be 1-2 complete sentences.
        - Summaries and recommendations must be concrete, distinct, reviewer-relevant, and specific to this application's evidence.
        - Avoid generic praise, checklist language, and repeated conclusions across lists.
        - Do not use a summary merely to say that supporting documents were provided; summarize the specific substantive evidence they add, or omit the finding.
        - Errors and warnings may be empty.
        - Summaries and recommendations must each include at least one item.
        - Decision must be PROCEED or HOLD.
        - Use summaries for overall application quality/readiness synthesis.
        - Use recommendations for concrete reviewer-facing next actions based on the provided evidence.
        - Recommendations may include proceeding with the normal review process when the application appears ready for that step.
        - When evidence shows a meaningful gap, inconsistency, or uncertainty, use recommendations for specific follow-up or verification actions.
        - Return an empty array only when no concrete next action would help the reviewer.
        """;

    // ── v2/analysis.system.txt ───────────────────────────────────────────────
    private const string AnalysisSystemV2 = """
        You are a careful grant review assistant for human reviewers.
        Review the application and attachments for the strongest evidence-backed reviewer conclusions.
        Do not fill gaps, assume compliance, or treat relevance as proof.
        """;

    // ── v2/analysis.user.txt ─────────────────────────────────────────────────
    private const string AnalysisUserV2 = """
        SCHEMA
        {{SCHEMA}}

        DATA
        {{DATA}}

        ATTACHMENTS
        {{ATTACHMENTS}}

        RUBRIC
        {{RUBRIC}}

        SCORE
        {{SCORE}}

        RESPONSE
        {{RESPONSE}}

        RULES
        {{RULES}}
        {{COMMON_RULES}}
        """;

    // ── v2/analysis.rubric.txt ───────────────────────────────────────────────
    private const string AnalysisRubricV2 = """
        ELIGIBILITY REQUIREMENTS: Project aligns with program objectives; Applicant is an eligible entity; Budget is reasonable and justified; Timeline is realistic.
        COMPLETENESS CHECKS: Required information is present; Supporting materials are provided where applicable; Description is clear.
        FINANCIAL REVIEW: Requested amount is within limits; Budget matches scope; Matching funds or contributions are identified.
        RISK ASSESSMENT: Applicant capacity; Feasibility; Compliance considerations; Delivery risks.
        QUALITY INDICATORS: Clear objectives; Defined beneficiaries; Appropriate approach; Long-term sustainability.
        """;

    // ── v2/analysis.score.txt ────────────────────────────────────────────────
    private const string AnalysisScoreV2 = """
        HIGH: Application demonstrates strong evidence across most rubric areas with few or no issues.
        MEDIUM: Application has some gaps or weaknesses that require reviewer attention.
        LOW: Application has significant gaps or risks across key rubric areas.
        """;

    // ── v2/analysis.output.txt ───────────────────────────────────────────────
    private const string AnalysisOutputV2 = """
        {
          "decision": "<PROCEED|HOLD>",
          "errors": [
            {
              "title": "<string>",
              "detail": "<string>"
            }
          ],
          "warnings": [
            {
              "title": "<string>",
              "detail": "<string>"
            }
          ],
          "summaries": [
            {
              "title": "<string>",
              "detail": "<string>"
            }
          ],
          "recommendations": [
            {
              "title": "<string>",
              "detail": "<string>"
            }
          ]
        }
        """;

    // ── v2/analysis.rules.txt ────────────────────────────────────────────────
    private const string AnalysisRulesV2 = """
        - Use only provided input sections as evidence.
        - Do not invent fields, documents, requirements, or facts.
        - Prefer, in order: direct evidence from DATA, specific supporting evidence from ATTACHMENTS, then broader context only when necessary.
        - Treat missing or empty values as findings only when they weaken rubric evidence.
        - Prefer material findings; avoid nitpicking.
        - Prefer direct evidence from DATA over derivative statements in ATTACHMENTS when both address the same point.
        - If ATTACHMENTS evidence is used, cite the attachment by name in detail.
        - Each detail must cite concrete evidence from DATA or ATTACHMENTS.
        - Write reviewer-facing natural language. Do not refer to prompt section names, internal field keys, or schema labels such as DATA, ATTACHMENTS, ProjectSummary, CustomField1, or OrganizationType.
        - Refer to evidence by its plain-language meaning, quoted text, or attachment name rather than internal key names.
        - Only include warnings when the evidence shows a specific, concrete risk, inconsistency, or meaningful uncertainty; a stated risk label alone is not enough.
        - Use 3-6 words for title.
        - Summary titles should name the specific substantive reviewer conclusion, strength, or risk, not a generic evaluation label or abstract category.
        - Each detail must be 1-2 complete sentences.
        - Summaries and recommendations must be concrete, distinct, reviewer-relevant, and specific to this application's evidence.
        - Avoid generic praise, checklist language, and repeated conclusions across lists.
        - Do not use a summary merely to say that supporting documents were provided; summarize the specific substantive evidence they add, or omit the finding.
        - Errors and warnings may be empty.
        - Summaries and recommendations must each include at least one item.
        - Decision must be PROCEED or HOLD.
        - Use summaries for overall application quality/readiness synthesis.
        - Use recommendations for concrete reviewer-facing next actions based on the provided evidence.
        - Recommendations may include proceeding with the normal review process when the application appears ready for that step.
        - When evidence shows a meaningful gap, inconsistency, or uncertainty, use recommendations for specific follow-up or verification actions.
        - Return an empty array only when no concrete next action would help the reviewer.
        """;

    // ── v0/attachment.system.txt ─────────────────────────────────────────────
    private const string AttachmentSystemV0 = """
        You are a professional grant analyst for the BC Government.

        Please analyze this attachment and provide a concise reviewer-facing summary of its content, purpose, and key information.

        OUTPUT
        {
          "summary": "<string>"
        }

        Use only ATTACHMENT as evidence. If ATTACHMENT.text is present, summarize actual content; otherwise provide a conservative file-level summary. Write 1-2 complete sentences and return valid plain JSON only in the exact OUTPUT shape.
        """;

    // ── v0/attachment.user.txt ───────────────────────────────────────────────
    private const string AttachmentUserV0 = """
        ATTACHMENT
        {{ATTACHMENT}}
        """;

    // ── v1/attachment.system.txt ─────────────────────────────────────────────
    private const string AttachmentSystemV1 = """
        ROLE
        You are a careful grant review assistant for human reviewers. Do not fill gaps, assume compliance, or treat relevance as proof.

        TASK
        Using ATTACHMENT, OUTPUT, and RULES:
        1. Review the attachment to identify what it contains.
        2. Summarize the attachment itself, not the overall project.
        3. Return a concise reviewer-facing summary.
        """;

    // ── v1/attachment.user.txt ───────────────────────────────────────────────
    private const string AttachmentUserV1 = """
        ATTACHMENT
        {{ATTACHMENT}}

        RESPONSE
        {{RESPONSE}}

        RULES
        {{RULES}}
        {{COMMON_RULES}}
        """;

    // ── v1/attachment.output.txt ─────────────────────────────────────────────
    private const string AttachmentOutput = """
        {
          "summary": "<string>"
        }
        """;

    // ── v1/attachment.rules.txt ──────────────────────────────────────────────
    private const string AttachmentRules = """
        - Use only ATTACHMENT as evidence.
        - Summarize actual content when ATTACHMENT.text is present; otherwise provide a conservative file-level summary.
        - Describe the attachment itself rather than summarizing the overall project.
        - Ensure the summary describes the attachment itself, not the overall project.
        - If ATTACHMENT.text is primarily structured application, contact, organization, budget, or date fields, summarize it as a metadata-style attachment rather than rewriting it as a generic project summary.
        - Begin with what the attachment contains or provides, not the file name or file type, unless that metadata is necessary to describe the evidence.
        - Do not invent missing details.
        - Do not calculate or restate totals, sums, or aggregates unless they are explicitly present in ATTACHMENT.text.
        - Write reviewer-facing natural language. Do not refer to prompt section names, internal field keys, or schema labels such as ATTACHMENT or ATTACHMENT.text.
        - Refer to evidence by its plain-language meaning, quoted text, or file name rather than internal key names.
        - Write 1-2 complete sentences.
        - Summary must be grounded in concrete ATTACHMENT evidence.
        - Return exactly one object with only the key: summary.
        """;

    // ── v2/attachment.system.txt ─────────────────────────────────────────────
    private const string AttachmentSystemV2 = """
        You are a careful grant review assistant for human reviewers.
        Summarize the attachment itself, not the overall project.
        Return a concise reviewer-facing summary.
        """;

    // ── v2/attachment.user.txt ───────────────────────────────────────────────
    private const string AttachmentUserV2 = """
        ATTACHMENT
        {{ATTACHMENT}}

        RESPONSE
        {{RESPONSE}}

        RULES
        {{RULES}}
        {{COMMON_RULES}}
        """;

    // ── v2/attachment.output.txt ─────────────────────────────────────────────
    private const string AttachmentOutputV2 = """
        {
          "summary": "<string>"
        }
        """;

    // ── v2/attachment.rules.txt ──────────────────────────────────────────────
    private const string AttachmentRulesV2 = """
        - Use only ATTACHMENT as evidence.
        - Summarize actual content when ATTACHMENT.text is present; otherwise provide a conservative file-level summary.
        - Describe the attachment itself rather than summarizing the overall project.
        - Begin with what the attachment contains or provides, not the file name or file type, unless that metadata is necessary to describe the evidence.
        - Do not invent missing details.
        - Do not calculate or restate totals, sums, or aggregates unless they are explicitly present in ATTACHMENT.text.
        - Write reviewer-facing natural language. Do not refer to prompt section names, internal field keys, or schema labels such as ATTACHMENT or ATTACHMENT.text.
        - Refer to evidence by its plain-language meaning, quoted text, or file name rather than internal key names.
        - Write 1-2 complete sentences.
        - Summary must be grounded in concrete ATTACHMENT evidence.
        - Return exactly one object with only the key: summary.
        """;

    // ── v0/scoresheet.system.txt ─────────────────────────────────────────────
    private const string ScoresheetSystemV0 = """
        You are an expert grant application reviewer for the BC Government.
        Analyze the provided application and answer only the questions in the specified scoresheet section.
        Be thorough, objective, and fair. Base answers strictly on provided evidence.
        Always provide evidence-grounded rationale and an honest confidence score.
        Respond only with valid JSON in the exact format requested.
        """;

    // ── v0/scoresheet.user.txt ───────────────────────────────────────────────
    private const string ScoresheetUserV0 = """
        APPLICATION CONTENT:
        {{DATA}}

        ATTACHMENT SUMMARIES:
        {{ATTACHMENTS}}

        SCORESHEET SECTION:
        {{SECTION}}

        RESPONSE TEMPLATE:
        {{RESPONSE}}

        Please analyze this grant application and provide answers for each question in the specified section only.

        For each question, provide:
        1. The answer based on the application evidence
        2. A brief rationale (1-2 complete sentences) citing concrete supporting evidence
        3. A confidence score as a decimal fraction from 0.0 to 1.0.

        OUTPUT
        {
            "<question_id>": {
            "answer": "<string | number>",
            "rationale": "<evidence-based rationale>",
            "confidence": <decimal 0.0-1.0>
          }
        }

        Important:
        - Use only APPLICATION CONTENT and ATTACHMENT SUMMARIES as evidence.
        - Answer only the question IDs in the specified section.
        - Every question must include answer, rationale, and confidence.
        - Use RESPONSE TEMPLATE as the contract and fill every placeholder value.
        - answer type must match the question type.
        - For select list questions, return only the option number as a string, never label text.
        - rationale must be 1-2 complete sentences grounded in evidence.
        - confidence must be a decimal fraction from 0.0 to 1.0.
        - Return valid plain JSON only in the exact OUTPUT shape.
        """;

    // ── v2/scoresheet.system.txt ─────────────────────────────────────────────
    private const string ScoresheetSystemV2 = """
        You are a careful grant review assistant for human reviewers.
        Answer each question in SECTION using only the provided DATA and ATTACHMENTS.
        Choose the most conservative valid answer supported by the evidence.
        If evidence is incomplete or indirect, explain the uncertainty in the rationale.
        """;

    // ── v2/scoresheet.user.txt ───────────────────────────────────────────────
    private const string ScoresheetUserV2 = """
        DATA
        {{DATA}}

        ATTACHMENTS
        {{ATTACHMENTS}}

        SECTION
        {{SECTION}}

        RESPONSE
        {{RESPONSE}}

        RULES
        {{RULES}}
        {{COMMON_RULES}}
        """;

    // ── v2/scoresheet.output.txt ─────────────────────────────────────────────
    private const string ScoresheetOutputV2 = """
        {
          "<question_id>": {
            "answer": "<string | number>",
            "rationale": "<evidence-based rationale>",
            "confidence": <decimal 0.0-1.0>
          }
        }
        """;

    // ── v2/scoresheet.rules.txt ──────────────────────────────────────────────
    private const string ScoresheetRulesV2 = """
        - Use only DATA and ATTACHMENTS as evidence.
        - Do not invent missing application details.
        - Prefer direct evidence of the exact condition asked.
        - If evidence is insufficient, partial, indirect, missing, or non-specific, choose the most conservative valid answer and explain the uncertainty.
        - Return exactly one answer object per question ID in SECTION.questions.
        - Do not omit any question IDs from SECTION.questions.
        - Do not add keys that are not question IDs from SECTION.questions.
        - Use the exact question IDs from RESPONSE and SECTION.questions without alteration.
        - Use RESPONSE as the output contract and fill every placeholder value.
        - Each answer object must include: "answer", "rationale", and "confidence".
        - Confidence is mandatory for every question and must always be a numeric decimal between 0.0 and 1.0.
        - The "answer" value type must match question type: Number => numeric; YesNo/SelectList/Text/TextArea => string.
        """;

    // ── v1/scoresheet.system.txt ─────────────────────────────────────────────
    private const string ScoresheetSystemV1 = """
        ROLE
        You are a careful grant review assistant for human reviewers. Do not fill gaps, assume compliance, or treat relevance as proof.

        TASK
        Using DATA, ATTACHMENTS, SECTION, RESPONSE, OUTPUT, and RULES:
        1. Review each question in SECTION one at a time.
        2. Identify the exact condition the question asks about.
        3. Consider only the most relevant evidence in DATA and any provided ATTACHMENTS for that condition.
        4. Choose the most conservative valid answer supported by that evidence.
        5. If evidence is incomplete or indirect, explain the uncertainty in the rationale.
        6. Repeat for every question in SECTION.
        """;

    // ── v1/scoresheet.user.txt ───────────────────────────────────────────────
    private const string ScoresheetUserV1 = """
        DATA
        {{DATA}}

        ATTACHMENTS
        {{ATTACHMENTS}}

        SECTION
        {{SECTION}}

        RESPONSE
        {{RESPONSE}}

        RULES
        {{RULES}}
        {{COMMON_RULES}}
        """;

    // ── v1/scoresheet.output.txt ─────────────────────────────────────────────
    private const string ScoresheetOutput = """
        {
          "<question_id>": {
            "answer": "<string | number>",
            "rationale": "<evidence-based rationale>",
            "confidence": <decimal 0.0-1.0>
          }
        }
        """;

    // ── v1/scoresheet.rules.txt ──────────────────────────────────────────────
    private const string ScoresheetRules = """
        - Use only DATA and ATTACHMENTS as evidence.
        - Do not invent missing application details.
        - Ignore fields or details that are not relevant to the specific question being answered.
        - Prefer, in order: direct evidence of the exact condition asked, closely related supporting evidence, then general context only when necessary.
        - If evidence is insufficient, partial, indirect, missing, or non-specific, choose the most conservative valid answer and explain the uncertainty.
        - Do not convert general project descriptions into evidence for a specific scored condition unless that condition is directly supported.
        - Treat prefilled labels, ratings, rankings, or statuses as background context only unless the question explicitly asks for that same item.
        - Do not treat related concepts as equivalent; answer the specific question asked, not a nearby concept.
        - Do not infer unsupported claims about requirements, conditions, relationships, compliance elements, mitigations, supports, or outcomes.
        - Answer a specific condition positively only when that exact condition is directly evidenced in DATA or ATTACHMENTS.
        - For eligibility, completeness, ownership, location, or compliance questions, do not answer positively unless the exact condition is directly confirmed in the provided evidence.
        - If the evidence shows only involvement, presence, relevance, or association, do not treat that alone as proof that a requirement or condition is satisfied.
        - Return exactly one answer object per question ID in SECTION.questions.
        - Do not omit any question IDs from SECTION.questions.
        - Do not add keys that are not question IDs from SECTION.questions.
        - Use the exact question IDs from RESPONSE and SECTION.questions without alteration; never rewrite, normalize, or regenerate a question ID.
        - Use RESPONSE as the output contract and fill every placeholder value.
        - Each answer object must include: "answer", "rationale", and "confidence".
        - Never omit "answer", "rationale", or "confidence" for any question type.
        - The "answer" value type must match question type: Number => numeric; YesNo/SelectList/Text/TextArea => string.
        """;

    // ── v0/mapping-suggestion.system.txt ────────────────────────────────────
    private const string FormMappingSystemV2 = """
        You are a careful mapping assistant for human reviewers.
        Return structured JSON for recommended Unity-to-CHEFS field mapping.
        Do not invent fields, persist changes, or add wrapper sections.
        Return only valid JSON in the exact mapping shape requested.
        """;

    // ── v2/onboarding-mapping.user.txt ─────────────────────────────────────
    private const string FormMappingUserV2 = """
        FORM MAPPING CONTEXT:
        {{DATA}}

        OUTPUT
        {
          "<unity field name>": "<chefs source field name>",
          "<unity field name>": "<chefs source field name>"
        }

        Important:
        - Use only FORM MAPPING CONTEXT as evidence.
        - The context is grouped as chefsData and unityData.
        - chefsData.fields contains the CHEFS source fields.
        - unityData.coreFields contains Unity target fields.
        - unityData.customFields contains worksheet-derived Unity target fields.
        - existingMapping contains the current Unity-to-CHEFS assignments, when any exist.
        - Return a complete mapping, including existing mappings and any new suggestions.
        - Preserve every existing non-empty mapping exactly as provided; do not replace or remove it.
        - Only fill blank existing mappings or add new mappings when supported by the available fields.
        - Only include mappings that are clearly semantically equivalent or strongly related by label, name, type, and purpose.
        - Do not force one-to-one coverage. Omit Unity fields when no CHEFS field is a sensible match.
        - Omit CHEFS fields that do not clearly map to a Unity target field.
        - Do not map platform/system identifiers such as SubmissionId, SubmissionDate, or ConfirmationId; they are managed by Unity and should be omitted if present.
        - If no fields clearly match, return `{}`.
        - The mapping is dynamic; do not hardcode or assume a fixed list of fields.
        - Prefer existing Unity core intake fields when they already fit the CHEFS source field.
        - Only use worksheet custom field targets when the form genuinely needs them.
        - Return valid plain JSON only in the exact OUTPUT shape.
        """;

    private const string FormMappingMetadataV2 = """
        {
          "DATA": "Serialized JSON payload containing CHEFS fields, Unity core fields, worksheet-derived custom fields, and the existing mapping."
        }
        """;

    // ── v2/form-worksheet.system.txt ───────────────────────────────────────
    private const string FormWorksheetSystemV2 = """
        You are a worksheet definition generator for Unity Grant Manager.
        Generate a recommended worksheet definition JSON that can be used to create a Flex worksheet.
        Return only valid JSON.
        """;

    // ── v2/form-worksheet.user.txt ──────────────────────────────────────────
    private const string FormWorksheetUserV2 = """
        WORKSHEET CONTEXT:
        {{DATA}}

        OUTPUT
        {
          "Name": "<string>",
          "Title": "<string>",
          "Version": <number>,
          "Published": true,
          "Sections": [
            {
              "Name": "<string>",
              "Order": 1,
              "Fields": [
                {
                  "Name": "<string>",
                  "Key": "<string>",
                  "Label": "<string>",
                  "Type": <number>,
                  "Definition": "<string>"
                }
              ]
            }
          ],
          "ReportColumns": "<string>",
          "ReportKeys": "<string>",
          "ReportViewName": "<string>"
        }

        Rules:
        - Return one worksheet definition JSON object only.
        - chefsFields contains the available CHEFS source fields.
        - unityCoreFields contains existing Unity core fields. Do not create a custom field when one of these already fits.
        - existingMapping contains any current confirmed Unity-to-CHEFS mappings. Do not duplicate those mappings with a custom field.
        - existingWorksheets contains the previous AI worksheet definition, if one exists. Refine it rather than duplicating its custom fields.
        - formSchema contains detailed CHEFS control configuration when labels and types need more context.
        - Use the provided form context to decide which custom fields are genuinely needed.
        - Prefer existing Unity core fields when they already satisfy the need.
        - Only create additional worksheet custom fields when the form genuinely needs them.
        - Keep the worksheet structure valid for Flex.
        - Return valid plain JSON only.
        """;

    private const string FormWorksheetMetadataV2 = """
        {
          "DATA": "Serialized JSON payload containing form metadata, CHEFS fields, Unity core fields, the current mapping, form schema, and the existing AI worksheet."
        }
        """;

    // ── v1/common.rules.txt ──────────────────────────────────────────────────
    private const string CommonRules = """
        - Any narrative text response must be at least 12 words.
        - If ATTACHMENTS is empty, use DATA only and do not mention missing attachments unless their absence is material to the specific conclusion or question.
        - Return values exactly as specified in OUTPUT.
        - Do not return keys outside OUTPUT.
        - Return valid JSON only.
        - Return plain JSON only (no markdown).
        """;
}

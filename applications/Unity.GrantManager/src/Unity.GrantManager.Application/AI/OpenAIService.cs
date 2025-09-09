using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.AI
{
    public class OpenAIService : IAIService, ITransientDependency
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAIService> _logger;
        private readonly ITextExtractionService _textExtractionService;

        private string? ApiKey => _configuration["AI:OpenAI:ApiKey"];
        private string? ApiUrl => _configuration["AI:OpenAI:ApiUrl"] ?? "https://api.openai.com/v1/chat/completions";
        private string Model => _configuration["AI:OpenAI:Model"] ?? "gpt-4o-mini";

        public OpenAIService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAIService> logger, ITextExtractionService textExtractionService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _textExtractionService = textExtractionService;
        }

        public async Task<bool> IsAvailableAsync()
        {
            return !string.IsNullOrEmpty(ApiKey);
        }

        public async Task<string> GenerateSummaryAsync(string content, string? prompt = null, int maxTokens = 150)
        {
            if (string.IsNullOrEmpty(ApiKey))
            {
                _logger.LogWarning("OpenAI API key is not configured");
                return "AI analysis not available - service not configured.";
            }

            try
            {
                var systemPrompt = prompt ?? "You are a professional grant analyst for the BC Government.";
                
                var requestBody = new
                {
                    model = Model,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = content }
                    },
                    max_tokens = maxTokens,
                    temperature = 0.3
                };

                var json = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");

                var response = await _httpClient.PostAsync(ApiUrl, httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OpenAI API request failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    return "AI analysis failed - service temporarily unavailable.";
                }

                using var jsonDoc = JsonDocument.Parse(responseContent);
                var choices = jsonDoc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    var message = choices[0].GetProperty("message");
                    return message.GetProperty("content").GetString() ?? "No summary generated.";
                }

                return "No summary generated.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI summary");
                return "AI analysis failed - please try again later.";
            }
        }

        public async Task<string> GenerateAttachmentSummaryAsync(string fileName, byte[] fileContent, string contentType)
        {
            try
            {
                // Try to extract text from the file
                var extractedText = await _textExtractionService.ExtractTextAsync(fileName, fileContent, contentType);
                
                string contentToAnalyze;
                string prompt;

                if (!string.IsNullOrWhiteSpace(extractedText))
                {
                    // We have extracted text - analyze the actual content
                    _logger.LogDebug("Extracted {TextLength} characters from {FileName}", extractedText.Length, fileName);
                    
                    contentToAnalyze = $"Document: {fileName}\nType: {contentType}\nContent:\n{extractedText}";
                    prompt = "Please analyze this document and provide a concise summary of its content, purpose, and key information, for use by your fellow grant analysts. It should be 1-2 sentences long and about 46 tokens.";
                }
                else
                {
                    // Fall back to filename and metadata analysis
                    _logger.LogDebug("No text extracted from {FileName}, analyzing metadata only", fileName);
                    
                    contentToAnalyze = $"File: {fileName}, Type: {contentType}, Size: {fileContent.Length} bytes";
                    prompt = "Please analyze this document and provide a concise summary of its content, purpose, and key information, for use by your fellow grant analysts. It should be 1-2 sentences long and about 46 tokens.";
                }
                
                return await GenerateSummaryAsync(contentToAnalyze, prompt, 150);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating attachment summary for {FileName}", fileName);
                return $"AI analysis not available for this attachment ({fileName}).";
            }
        }

        public async Task<string> AnalyzeApplicationAsync(string applicationContent, List<string> attachmentSummaries, string rubric)
        {
            if (string.IsNullOrEmpty(ApiKey))
            {
                _logger.LogWarning("OpenAI API key is not configured");
                return "AI analysis not available - service not configured.";
            }

            try
            {
                var attachmentSummariesText = attachmentSummaries?.Any() == true 
                    ? string.Join("\n- ", attachmentSummaries.Select((s, i) => $"Attachment {i + 1}: {s}"))
                    : "No attachments provided.";

                var analysisContent = $@"APPLICATION CONTENT:
{applicationContent}

ATTACHMENT SUMMARIES:
- {attachmentSummariesText}

EVALUATION RUBRIC:
{rubric}

Please analyze this grant application against the provided rubric and return your findings in the following JSON format:
{{
  ""overall_score"": ""HIGH/MEDIUM/LOW"",
  ""warnings"": [
    {{
      ""category"": ""Category Name"",
      ""message"": ""Specific warning message"",
      ""severity"": ""WARNING""
    }}
  ],
  ""errors"": [
    {{
      ""category"": ""Category Name"",
      ""message"": ""Specific error message"",
      ""severity"": ""ERROR""
    }}
  ],
  ""recommendations"": [
    ""Specific recommendation for improvement""
  ]
}}";

                var systemPrompt = @"You are an expert grant application reviewer for the BC Government. 
Analyze the provided application against the rubric and identify any issues, missing requirements, or areas of concern. 
Be thorough but fair in your assessment. Focus on compliance, completeness, and alignment with program requirements.
Respond only with valid JSON in the exact format requested.";

                return await GenerateSummaryAsync(analysisContent, systemPrompt, 1000);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing application");
                return "AI analysis failed - please try again later.";
            }
        }

        public async Task<string> GenerateScoresheetAnswersAsync(string applicationContent, List<string> attachmentSummaries, string scoresheetQuestions)
        {
            if (string.IsNullOrEmpty(ApiKey))
            {
                _logger.LogWarning("OpenAI API key is not configured");
                return "{}";
            }

            try
            {
                var attachmentSummariesText = attachmentSummaries?.Any() == true 
                    ? string.Join("\n- ", attachmentSummaries.Select((s, i) => $"Attachment {i + 1}: {s}"))
                    : "No attachments provided.";

                var analysisContent = $@"APPLICATION CONTENT:
{applicationContent}

ATTACHMENT SUMMARIES:
- {attachmentSummariesText}

SCORESHEET QUESTIONS:
{scoresheetQuestions}

Please analyze this grant application and provide appropriate answers for each scoresheet question. 

For numeric questions, provide a numeric value within the specified range.
For yes/no questions, provide either 'Yes' or 'No'.
For text questions, provide a concise, relevant response.
For select list questions, choose the most appropriate option from the provided choices.
For text area questions, provide a detailed but concise response.

Base your answers on the application content and attachment summaries provided. Be objective and fair in your assessment.

Return your response as a JSON object where each key is the question ID and the value is the appropriate answer:
{{
  ""question-id-1"": ""answer-value-1"",
  ""question-id-2"": ""answer-value-2""
}}
Do not return any markdown formatting, just the JSON by itself";

                var systemPrompt = @"You are an expert grant application reviewer for the BC Government. 
Analyze the provided application and generate appropriate answers for the scoresheet questions based on the application content.
Be thorough, objective, and fair in your assessment. Base your answers strictly on the provided application content.
Respond only with valid JSON in the exact format requested.";

                return await GenerateSummaryAsync(analysisContent, systemPrompt, 2000);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating scoresheet answers");
                return "{}";
            }
        }

        public async Task<string> GenerateScoresheetSectionAnswersAsync(string applicationContent, List<string> attachmentSummaries, string sectionJson, string sectionName)
        {
            if (string.IsNullOrEmpty(ApiKey))
            {
                _logger.LogWarning("OpenAI API key is not configured");
                return "{}";
            }

            try
            {
                var attachmentSummariesText = attachmentSummaries?.Any() == true 
                    ? string.Join("\n- ", attachmentSummaries.Select((s, i) => $"Attachment {i + 1}: {s}"))
                    : "No attachments provided.";

                var analysisContent = $@"APPLICATION CONTENT:
{applicationContent}

ATTACHMENT SUMMARIES:
- {attachmentSummariesText}

SCORESHEET SECTION: {sectionName}
{sectionJson}

Please analyze this grant application and provide appropriate answers for each question in the ""{sectionName}"" section only. 

For each question, provide:
1. Your answer based on the application content
2. A brief cited description (1-2 sentences) explaining your reasoning with specific references to the application content
3. A confidence score from 0-100 indicating how confident you are in your answer based on available information

Guidelines for answers:
- For numeric questions, provide a numeric value within the specified range
- For yes/no questions, provide either 'Yes' or 'No'
- For text questions, provide a concise, relevant response
- For select list questions, respond with ONLY the number (1, 2, 3, etc.) corresponding to your chosen option from the numbered availableOptions list. Do not include the text of the option, just the number.
- For text area questions, provide a detailed but concise response
- Base your confidence score on how clearly the application content supports your answer

Return your response as a JSON object where each key is the question ID and the value contains the answer, citation, and confidence:
{{
  ""question-id-1"": {{
    ""answer"": ""your-answer-here"",
    ""citation"": ""Brief explanation with specific reference to application content"",
    ""confidence"": 85
  }},
  ""question-id-2"": {{
    ""answer"": ""3"",
    ""citation"": ""Based on the project budget of $50,000 mentioned in the application, this falls into the medium budget category"",
    ""confidence"": 90
  }}
}}

IMPORTANT FOR SELECT LIST QUESTIONS: If a question has availableOptions like:
[{{""number"":1,""value"":""Low (Under $25K)""}}, {{""number"":2,""value"":""Medium ($25K-$75K)""}}, {{""number"":3,""value"":""High (Over $75K)""}}]
Then respond with ONLY the number (e.g., ""3"" for ""High (Over $75K)""), not the text value.

Do not return any markdown formatting, just the JSON by itself";

                var systemPrompt = @"You are an expert grant application reviewer for the BC Government. 
Analyze the provided application and generate appropriate answers for the scoresheet section questions based on the application content.
Be thorough, objective, and fair in your assessment. Base your answers strictly on the provided application content.
Always provide citations that reference specific parts of the application content to support your answers.
Be honest about your confidence level - if information is missing or unclear, reflect this in a lower confidence score.
Respond only with valid JSON in the exact format requested.";

                return await GenerateSummaryAsync(analysisContent, systemPrompt, 2000);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating scoresheet section answers for section {SectionName}", sectionName);
                return "{}";
            }
        }
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.GrantManager.AI
{
    /// <summary>
    /// Interface for AI/LLM services used throughout the application
    /// </summary>
    public interface IAIService
    {
        /// <summary>
        /// Generates a summary for the given content using AI/LLM
        /// </summary>
        /// <param name="content">The content to summarize</param>
        /// <param name="prompt">Optional custom prompt for the AI</param>
        /// <param name="maxTokens">Maximum number of tokens in the response</param>
        /// <returns>AI-generated summary</returns>
        Task<string> GenerateSummaryAsync(string content, string? prompt = null, int maxTokens = 150);

        /// <summary>
        /// Generates a summary for an attachment based on its content and metadata
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        /// <param name="fileContent">Binary content of the file</param>
        /// <param name="contentType">MIME type of the file</param>
        /// <returns>AI-generated summary of the attachment</returns>
        Task<string> GenerateAttachmentSummaryAsync(string fileName, byte[] fileContent, string contentType);

        /// <summary>
        /// Checks if the AI service is available and configured
        /// </summary>
        /// <returns>True if the service is ready to use</returns>
        Task<bool> IsAvailableAsync();

        /// <summary>
        /// Analyzes an application against a rubric and generates warnings/errors
        /// </summary>
        /// <param name="applicationContent">The application form content</param>
        /// <param name="attachmentSummaries">List of AI-generated attachment summaries</param>
        /// <param name="rubric">The evaluation rubric to apply</param>
        /// <returns>Structured analysis with warnings and errors</returns>
        Task<string> AnalyzeApplicationAsync(string applicationContent, List<string> attachmentSummaries, string rubric);
    }
}
using System.Threading.Tasks;

namespace Unity.GrantManager.AI
{
    /// <summary>
    /// Service for extracting text from various file formats
    /// </summary>
    public interface ITextExtractionService
    {
        /// <summary>
        /// Extracts text content from a file based on its type
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        /// <param name="fileContent">Binary content of the file</param>
        /// <param name="contentType">MIME type of the file</param>
        /// <returns>Extracted text content or null if extraction not supported</returns>
        Task<string?> ExtractTextAsync(string fileName, byte[] fileContent, string contentType);

        /// <summary>
        /// Checks if text extraction is supported for the given file type
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        /// <param name="contentType">MIME type of the file</param>
        /// <returns>True if text extraction is supported</returns>
        bool IsExtractionSupported(string fileName, string contentType);
    }
}
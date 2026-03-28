using System.Threading.Tasks;

namespace Unity.AI.Extraction
{
    public interface ITextExtractionService
    {
        Task<string> ExtractTextAsync(string fileName, byte[] fileContent, string contentType);
    }
}

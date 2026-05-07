using System.IO;
using System.Threading.Tasks;

namespace Unity.AI.Extraction
{
    public interface ITextExtractionService
    {
        Task<string> ExtractTextAsync(string fileName, Stream fileContent, string contentType);
    }
}

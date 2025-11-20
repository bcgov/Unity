using System.Threading.Tasks;

namespace Unity.GrantManager.AI
{
    public interface ITextExtractionService
    {
        Task<string> ExtractTextAsync(string fileName, byte[] fileContent, string contentType);
    }
}

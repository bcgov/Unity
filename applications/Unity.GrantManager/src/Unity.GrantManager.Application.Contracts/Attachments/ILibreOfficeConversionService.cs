using System.Threading.Tasks;

namespace Unity.GrantManager.Attachments;

public interface ILibreOfficeConversionService
{
    bool IsInstalled();
    Task<byte[]> ConvertToPdfAsync(byte[] fileContent, string fileName);
}

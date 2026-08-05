using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Requests;
using Unity.AI.Responses;

namespace Unity.AI.Operations;

public interface IFormWorksheetService
{
    Task<FormWorksheetResponse> GenerateFormWorksheetAsync(FormWorksheetRequest request, CancellationToken cancellationToken = default);
}

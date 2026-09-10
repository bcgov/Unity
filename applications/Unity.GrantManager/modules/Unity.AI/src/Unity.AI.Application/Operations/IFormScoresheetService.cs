using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Requests;
using Unity.AI.Responses;

namespace Unity.AI.Operations;

public interface IFormScoresheetService
{
    Task<FormScoresheetResponse> GenerateFormScoresheetAsync(FormScoresheetRequest request, CancellationToken cancellationToken = default);
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Requests;
using Unity.AI.Responses;

namespace Unity.AI.Operations;

public interface IFormMappingService
{
    Task<FormMappingResponse> GenerateFormMappingAsync(FormMappingRequest request, CancellationToken cancellationToken = default);
}

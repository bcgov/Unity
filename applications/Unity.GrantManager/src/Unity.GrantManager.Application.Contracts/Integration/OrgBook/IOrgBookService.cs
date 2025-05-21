using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Integrations.Orgbook
{
    public interface IOrgBookService : IApplicationService
    {
        Task<dynamic?> GetOrgBookQueryAsync(string orgBookQuery);

        Task<JsonDocument> GetOrgBookAutocompleteQueryAsync(string? orgBookQuery);
        Task<JsonDocument> GetOrgBookDetailsQueryAsync(string? orgBookId);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Integrations
{
    [RemoteService(false)]
    public class CasClientCodeLookupService(ICasClientCodeRepository repository) : ApplicationService, ICasClientCodeLookupService
    {

        public async Task<List<CasClientCodeOptionDto>> GetActiveOptionsAsync()
        {
            var codes = await repository.GetListAsync();
            
            return [.. codes
                .Where(c => c.IsActive)
                .OrderBy(c => c.ClientCode)
                .Select(c => new CasClientCodeOptionDto
                {
                    Code = c.ClientCode,
                    DisplayName = $"{c.ClientCode} - {c.Description}",
                    Ministry = c.FinancialMinistry ?? string.Empty
                })];
        }

        public async Task<string?> GetClientIdByCasClientCodeAsync(string casClientCode)
        {
            if (string.IsNullOrWhiteSpace(casClientCode))
            {
                return null;
            }

            var queryable = await repository.GetQueryableAsync();
            var code = await AsyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.ClientCode == casClientCode)
            );

            return code?.ClientId;
        }

    }
}
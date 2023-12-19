using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.GrantManager.UserImport
{
    public interface IUserImportAppService
    {
        public Task<IList<ImportUserDto>> SearchAsync(ImportUserSearchDto importUserSearchDto);
    }
}

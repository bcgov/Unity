using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Integration.Sso;
using Volo.Abp;
using Volo.Abp.Identity;
using Volo.Abp.Validation;

namespace Unity.GrantManager.UserImport
{
    [Authorize(IdentityPermissions.Users.Default)]
    public class UserImportAppService : GrantManagerAppService, IUserImportAppService
    {
        private readonly ISsoUsersApiService _ssoUsersApiService;

        public UserImportAppService(ISsoUsersApiService ssoUsersApiService)
        {
            _ssoUsersApiService = ssoUsersApiService;
        }

        public async Task<IList<ImportUserDto>> SearchAsync(ImportUserSearchDto importUserSearchDto)
        {
            var users = new List<ImportUserDto>();

            importUserSearchDto.Directory = importUserSearchDto.Directory.Trim().ToLower();
            if (importUserSearchDto.Directory != "idir") throw new AbpValidationException("Idir Only",
                new List<ValidationResult>()
                { 
                    new ValidationResult("Only idir search is supported")
                }); // for now

            if (string.IsNullOrEmpty(importUserSearchDto.FirstName) && string.IsNullOrEmpty(importUserSearchDto.LastName))
                return users;

            if (!string.IsNullOrEmpty(importUserSearchDto.FirstName) && importUserSearchDto.FirstName.Length < 2)
            {
                throw new AbpValidationException("Validation Error", 
                    new List<ValidationResult>()
                    {
                        new ValidationResult("First name length must be greater than 2")
                    });
            }

            if (!string.IsNullOrEmpty(importUserSearchDto.LastName) && importUserSearchDto.LastName.Length < 2)
            {
                throw new AbpValidationException("Validation Error",
                    new List<ValidationResult>()
                    {
                        new ValidationResult("Last name length must be greater than 2")
                    });
            }

            var result = await _ssoUsersApiService.SearchUsersAsync(importUserSearchDto.Directory, importUserSearchDto.FirstName, importUserSearchDto.LastName);

            if (result.Success == false) throw new UserFriendlyException("Error searching directory users");

            if (result.Success && result.Data != null)
            {
                foreach (var item in result.Data)
                {
                    users.Add(new ImportUserDto()
                    {
                        FirstName = item.FirstName,
                        LastName = item.LastName,
                        UserGuid = item.Attributes?.IdirUserGuid?.FirstOrDefault() ?? string.Empty,
                        Username = item.Attributes?.IdirUsername?.FirstOrDefault() ?? string.Empty,
                        DisplayName = item.Attributes?.DisplayName?.FirstOrDefault() ?? string.Empty
                    });
                }
            }

            return users;
        }
    }
}

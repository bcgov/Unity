using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Integration.Sso;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Validation;

namespace Unity.GrantManager.Identity
{
    [Authorize(IdentityPermissions.Users.Default)]
    public class UserImportAppService : GrantManagerAppService, IUserImportAppService
    {
        private readonly ISsoUsersApiService _ssoUsersApiService;
        private readonly ICurrentTenant _currentTenant;
        private readonly IdentityUserManager _userManager;
        private readonly IPersonRepository _personRepository;
        private readonly IIdentityUserRepository _identityUserRepository;
        private readonly IDataFilter _dataFilter;

        public UserImportAppService(ISsoUsersApiService ssoUsersApiService,
            ICurrentTenant currentTenant,
            IdentityUserManager userManager,
            IPersonRepository personRepository,
            IIdentityUserRepository identityUserRepository,
            IDataFilter dataFilter)
        {
            _ssoUsersApiService = ssoUsersApiService;
            _currentTenant = currentTenant;
            _userManager = userManager;
            _personRepository = personRepository;
            _identityUserRepository = identityUserRepository;
            _dataFilter = dataFilter;
        }

        public async Task ImportUserAsync(ImportUserDto importUserDto)
        {
            var newUserId = Guid.NewGuid();

            var result = await _ssoUsersApiService.FindUserAsync(importUserDto.Directory, importUserDto.Guid);

            if (result.Data == null || result.Data.Length == 0) throw new AbpValidationException();

            var ssoUser = result.Data[0];

            IdentityUser? identityUser = await ReactivateAndGetDeletedUserAsync(ssoUser);
            identityUser ??= await CreateNewIdentityUserAsync(newUserId, ssoUser);

            if (identityUser == null) throw new UserFriendlyException("Error creating user account");

            await _userManager.AddDefaultRolesAsync(identityUser);

            var oicdSub = ssoUser.Username ?? newUserId.ToString();
            var displayName = ssoUser.Attributes?.DisplayName?[0] ?? identityUser.NormalizedUserName.ToString();

            await UpdateAdditionalUserPropertiesAsync(identityUser, oicdSub, displayName);
            await SyncUserToCurrentTenantAsync(newUserId, identityUser, oicdSub, displayName);
        }

        private async Task<IdentityUser?> CreateNewIdentityUserAsync(Guid newUserId, SsoUser ssoUser)
        {
            IdentityUser? identityUser = new(newUserId, ssoUser.Attributes?.IdirUsername?[0], ssoUser.Email ?? $"{ssoUser.Attributes?.IdirUsername}@{ssoUser.Attributes?.IdirUsername}.com", _currentTenant.Id)
            {
                Name = ssoUser.FirstName,
                Surname = ssoUser.LastName
            };
            identityUser.SetEmailConfirmed(true);

            // Use identity user manager to create the user
            var createUserResult = await _userManager.CreateAsync(identityUser) ?? throw new AbpException("Unxpected error importing user");

            if (!createUserResult.Succeeded)
            {
                var validationErrors = new List<ValidationResult>();
                foreach (var error in createUserResult.Errors)
                {
                    validationErrors.Add(new ValidationResult($"{error.Code} {error.Description}"));
                }
                throw new AbpValidationException("Error importing user", validationErrors);
            }

            return identityUser;
        }

        private async Task<IdentityUser?> ReactivateAndGetDeletedUserAsync(SsoUser ssoUser)
        {
            //Temporary disable the ISoftDelete filter - find delete user account and reactivate for import
            using (_dataFilter.Disable<ISoftDelete>())
            {
                var identityUser = await _identityUserRepository
                    .FindByTenantIdAndUserNameAsync(ssoUser.Attributes?.IdirUsername?[0], _currentTenant.Id);

                if (identityUser != null)
                {
                    identityUser.IsDeleted = false;
                    identityUser.DeleterId = null;
                    identityUser.DeletionTime = null;
                    await _identityUserRepository.UpdateAsync(identityUser);
                    return identityUser;
                }
            }

            return null;
        }

        public async Task<IList<UserDto>> SearchAsync(UserSearchDto importUserSearchDto)
        {
            var users = new List<UserDto>();

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

            if (!result.Success) throw new UserFriendlyException("Error searching directory users");

            if (result.Success && result.Data != null)
            {
                foreach (var item in result.Data)
                {
                    users.Add(new UserDto()
                    {
                        FirstName = item.FirstName,
                        LastName = item.LastName,
                        UserGuid = item.Attributes?.IdirUserGuid?.FirstOrDefault() ?? string.Empty,
                        Username = item.Attributes?.IdirUsername?.FirstOrDefault() ?? string.Empty,
                        DisplayName = item.Attributes?.DisplayName?.FirstOrDefault() ?? string.Empty,
                        OidcSub = item.Username
                    });
                }
            }

            return users;
        }

        private async Task SyncUserToCurrentTenantAsync(Guid userId, IdentityUser user, string oidcSub, string displayName)
        {
            var existingUser = await _personRepository.FindByOidcSub(oidcSub);
            if (existingUser == null)
            {
                await _personRepository.InsertAsync(new Person()
                {
                    Id = userId,
                    OidcSub = oidcSub,
                    OidcDisplayName = displayName,
                    FullName = $"{user.Name} {user.Surname}",
                    Badge = Utils.CreateUserBadge(user)
                });
            }
        }

        private async Task<IdentityUser> UpdateAdditionalUserPropertiesAsync(IdentityUser user, string oidcSub, string displayName)
        {
            if (user != null)
            {
                user.SetProperty("OidcSub", oidcSub);
                user.SetProperty("DisplayName", displayName);
                await _identityUserRepository.UpdateAsync(user, true);
            }

            return user!;
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Integration.Css;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Validation;

namespace Unity.GrantManager.Identity
{
    [Authorize(IdentityPermissions.Users.Create)]
    public class UserImportAppService : GrantManagerAppService, IUserImportAppService
    {
        private readonly ICssUsersApiService _cssUsersApiService;
        private readonly ICurrentTenant _currentTenant;
        private readonly IdentityUserManager _userManager;
        private readonly IPersonRepository _personRepository;
        private readonly IIdentityUserRepository _identityUserRepository;
        private readonly IDataFilter _dataFilter;

        public UserImportAppService(ICssUsersApiService cssUsersApiService,
            ICurrentTenant currentTenant,
            IdentityUserManager userManager,
            IPersonRepository personRepository,
            IIdentityUserRepository identityUserRepository,
            IDataFilter dataFilter)
        {
            _cssUsersApiService = cssUsersApiService;
            _currentTenant = currentTenant;
            _userManager = userManager;
            _personRepository = personRepository;
            _identityUserRepository = identityUserRepository;
            _dataFilter = dataFilter;
        }

        /// <summary>
        /// Import a user creating both a host account and a local person entity for the relevant tenant
        /// </summary>
        /// <param name="importUserDto"></param>
        /// <returns></returns>
        /// <exception cref="AbpValidationException"></exception>
        /// <exception cref="UserFriendlyException"></exception>
        public async Task ImportUserAsync(ImportUserDto importUserDto)
        {
            var newUserId = Guid.NewGuid();

            var result = await _cssUsersApiService.FindUserAsync(importUserDto.Directory, importUserDto.Guid);

            if (result.Data == null || result.Data.Length == 0) throw new AbpValidationException();

            var cssUser = result.Data[0];

            IdentityUser? identityUser = await ReactivateAndGetDeletedUserAsync(cssUser.Attributes?.IdirUsername?[0] ?? throw new AbpValidationException());
            identityUser ??= await CreateNewIdentityUserAsync(newUserId, cssUser.Attributes?.IdirUsername?[0], cssUser.FirstName, cssUser.LastName, cssUser.Email);

            if (identityUser == null) throw new UserFriendlyException("Error creating user account");

            await _userManager.AddDefaultRolesAsync(identityUser);

            if (importUserDto.Roles?.Length > 0)
            {
                await _userManager.AddToRolesAsync(identityUser, importUserDto.Roles);
            }

            var oidcSub = (cssUser.Attributes?.IdirUserGuid?[0] ?? newUserId.ToString()).ToSubjectWithoutIdp();
            var displayName = cssUser.Attributes?.DisplayName?[0] ?? identityUser.NormalizedUserName.ToString();

            await UpdateAdditionalUserPropertiesAsync(identityUser, oidcSub, displayName);
            await SyncUserToCurrentTenantAsync(newUserId, identityUser, oidcSub, displayName);
        }

        /// <summary>
        /// Allow internal non authenticated requests to also import users when required
        /// </summary>
        /// <param name="importUserDto"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [RemoteService(false)]
        public async Task AutoImportUserInternalAsync(ImportUserDto importUserDto, 
            string username,
            string firstName,
            string lastName, 
            string emailAddress,
            string oidcSub, 
            string displayName)
        {
            var newUserId = Guid.NewGuid();                        

            IdentityUser? identityUser = await ReactivateAndGetDeletedUserAsync(username);
            identityUser ??= await CreateNewIdentityUserAsync(newUserId, username, firstName, lastName, emailAddress);

            if (identityUser == null) throw new UserFriendlyException("Error creating user account");

            await _userManager.AddDefaultRolesAsync(identityUser);

            if (importUserDto.Roles?.Length > 0)
            {
                await _userManager.AddToRolesAsync(identityUser, importUserDto.Roles);
            }
            
            await UpdateAdditionalUserPropertiesAsync(identityUser, oidcSub, displayName);
            await SyncUserToCurrentTenantAsync(newUserId, identityUser, oidcSub, displayName);
        }

        /// <summary>
        /// Search against the single sign on service for a user
        /// </summary>
        /// <param name="importUserSearchDto"></param>
        /// <returns></returns>
        /// <exception cref="AbpValidationException"></exception>
        /// <exception cref="UserFriendlyException"></exception>
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

            var result = await _cssUsersApiService.SearchUsersAsync(importUserSearchDto.Directory, importUserSearchDto.FirstName, importUserSearchDto.LastName);

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

        private async Task<IdentityUser?> CreateNewIdentityUserAsync(Guid newUserId, string? username, string? firstName, string? lastName, string? emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress)) emailAddress = null;
            IdentityUser? identityUser = new(newUserId, username, emailAddress ?? $"{Guid.Empty}@{username}", _currentTenant.Id)
            {
                Name = firstName,
                Surname = lastName,
            };

            if (emailAddress != null)
            {
                identityUser.SetEmailConfirmed(true);
            }            

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

        private async Task<IdentityUser?> ReactivateAndGetDeletedUserAsync(string username)
        {
            //Temporary disable the ISoftDelete filter - find delete user account and reactivate for import
            using (_dataFilter.Disable<ISoftDelete>())
            {
                var identityUser = await _identityUserRepository
                    .FindByTenantIdAndUserNameAsync(username, _currentTenant.Id);

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

        private async Task SyncUserToCurrentTenantAsync(Guid userId, IdentityUser user, string oidcSub, string displayName)
        {
            var existingUser = await _personRepository.FindByOidcSub(oidcSub);            
            if (existingUser == null)
            {
                await _personRepository.InsertAsync(new Person()
                {
                    Id = userId,
                    OidcSub = oidcSub.ToSubjectWithoutIdp(),
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
                user.SetProperty("OidcSub", oidcSub.ToSubjectWithoutIdp());
                user.SetProperty("DisplayName", displayName);
                await _identityUserRepository.UpdateAsync(user, true);
            }

            return user!;
        }
    }
}
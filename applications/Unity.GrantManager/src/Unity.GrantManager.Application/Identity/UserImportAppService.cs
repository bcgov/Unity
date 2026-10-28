using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Integrations.Css;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Validation;

namespace Unity.GrantManager.Identity
{
    public class UserImportAppService(ICssUsersApiService cssUsersApiService,
            ICurrentTenant currentTenant,
            IdentityUserManager userManager,
            IPersonRepository personRepository,
            IUserAccountsRepository userAccountsRepository,
            IIdentityUserRepository identityUserRepository,
            IDataFilter dataFilter) : GrantManagerAppService, IUserImportAppService
    {

        /// <summary>
        /// Import a user creating both a host account and a local person entity for the relevant tenant
        /// </summary>
        /// <param name="importUserDto"></param>
        /// <returns></returns>
        /// <exception cref="AbpValidationException"></exception>
        /// <exception cref="UserFriendlyException"></exception>
        [Authorize(IdentityPermissions.Users.Create)]
        public async Task ImportUserAsync(ImportUserDto importUserDto)
        {
            var result = await cssUsersApiService.FindUserAsync(importUserDto.Directory, importUserDto.Guid);

            if (result.Data == null || result.Data.Length == 0) throw new AbpValidationException();

            var cssUser = result.Data[0];
            var oidcSub = (cssUser.Attributes?.IdirUserGuid?[0] ?? Guid.NewGuid().ToString()).ToSubjectWithoutIdp();
            var existingPerson = await personRepository.FindByOidcSub(oidcSub);
            var newUserId = existingPerson?.Id ?? Guid.NewGuid();

            IdentityUser? identityUser = await ReactivateAndGetDeletedUserAsync(
                cssUser.Attributes?.IdirUsername?[0] ?? throw new AbpValidationException(), oidcSub, cssUser.FirstName, cssUser.LastName);
            identityUser ??= await CreateNewIdentityUserAsync(newUserId, cssUser.Attributes?.IdirUsername?[0], cssUser.FirstName, cssUser.LastName, cssUser.Email);

            if (identityUser == null) throw new UserFriendlyException("Error creating user account");

            await userManager.AddDefaultRolesAsync(identityUser);

            if (importUserDto.Roles?.Length > 0)
            {
                await userManager.AddToRolesAsync(identityUser, importUserDto.Roles);
            }

            var displayName = cssUser.Attributes?.DisplayName?[0] ?? identityUser.NormalizedUserName.ToString();

            await UpdateAdditionalUserPropertiesAsync(identityUser, oidcSub, displayName);
            await SyncUserToCurrentTenantAsync(identityUser, oidcSub, displayName);
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
            var normalizedOidcSub = oidcSub.ToSubjectWithoutIdp();
            var existingPerson = await personRepository.FindByOidcSub(normalizedOidcSub);
            var newUserId = existingPerson?.Id ?? Guid.NewGuid();

            IdentityUser? identityUser = await ReactivateAndGetDeletedUserAsync(username, normalizedOidcSub, firstName, lastName);
            identityUser ??= await CreateNewIdentityUserAsync(newUserId, username, firstName, lastName, emailAddress);

            if (identityUser == null) throw new UserFriendlyException("Error creating user account");

            await userManager.AddDefaultRolesAsync(identityUser);

            if (importUserDto.Roles?.Length > 0)
            {
                await userManager.AddToRolesAsync(identityUser, importUserDto.Roles);
            }
            
            await UpdateAdditionalUserPropertiesAsync(identityUser, normalizedOidcSub, displayName);
            await SyncUserToCurrentTenantAsync(identityUser, normalizedOidcSub, displayName);
        }

        /// <summary>
        /// Search against the single sign on service for a user
        /// </summary>
        /// <param name="importUserSearchDto"></param>
        /// <returns></returns>
        /// <exception cref="AbpValidationException"></exception>
        /// <exception cref="UserFriendlyException"></exception>
        [Authorize(IdentityPermissions.Users.Create)]
        public async Task<IList<UserDto>> SearchAsync(UserSearchDto importUserSearchDto)
        {
            var users = new List<UserDto>();

            importUserSearchDto.Directory = importUserSearchDto.Directory.Trim().ToLower();
            if (importUserSearchDto.Directory != "idir") throw new AbpValidationException("Idir Only",
                [
                    new("Only idir search is supported")
                ]); // for now

            if (string.IsNullOrEmpty(importUserSearchDto.FirstName) &&
                string.IsNullOrEmpty(importUserSearchDto.LastName) &&
                string.IsNullOrEmpty(importUserSearchDto.Email))
                return users;

            if (!string.IsNullOrEmpty(importUserSearchDto.FirstName) && importUserSearchDto.FirstName.Length < 2)
            {
                throw new AbpValidationException("Validation Error",
                    [
                        new ValidationResult("First name length must be greater than 2")
                    ]);
            }

            if (!string.IsNullOrEmpty(importUserSearchDto.LastName) && importUserSearchDto.LastName.Length < 2)
            {
                throw new AbpValidationException("Validation Error",
                    [
                        new ValidationResult("Last name length must be greater than 2")
                    ]);
            }

            if (!string.IsNullOrEmpty(importUserSearchDto.Email) && importUserSearchDto.Email.Length < 2)
            {
                throw new AbpValidationException("Validation Error",
                    [
                        new ValidationResult("Email length must be greater than 2")
                    ]);
            }

            var result = await cssUsersApiService.SearchUsersAsync(importUserSearchDto.Directory, importUserSearchDto.FirstName, importUserSearchDto.LastName, importUserSearchDto.Email);

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
                        OidcSub = item.Username,
                        Email = item.Email
                    });
                }
            }

            return [.. users.GroupBy(u => u.UserGuid).Select(g => g.First())];
        }

        [Authorize(IdentityPermissions.Users.Update)]
        public async Task<IList<UserDto>> SearchForRenameAsync(RenameUserSearchDto renameUserSearchDto)
        {
            var person = await personRepository.GetAsync(renameUserSearchDto.UserId);
            if (string.IsNullOrWhiteSpace(person.OidcSub))
            {
                return [];
            }
            var oidcSub = person.OidcSub.ToSubjectWithoutIdp();
            var result = await cssUsersApiService.FindUserAsync(renameUserSearchDto.Directory, oidcSub);
            if (!result.Success || result.Data == null)
            {
                return [];
            }

            return [.. result.Data
                .Where(user => SubjectsMatch(user.Attributes?.IdirUserGuid?.FirstOrDefault(), person.OidcSub))
                .Select(user => new UserDto
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    UserGuid = user.Attributes?.IdirUserGuid?.FirstOrDefault() ?? string.Empty,
                    Username = user.Attributes?.IdirUsername?.FirstOrDefault() ?? string.Empty,
                    DisplayName = user.Attributes?.DisplayName?.FirstOrDefault() ?? string.Empty,
                    OidcSub = user.Username,
                    Email = user.Email
                })];
        }

        [Authorize(IdentityPermissions.Users.Update)]
        public async Task RenameUserAsync(RenameUserDto renameUserDto)
        {
            var person = await personRepository.GetAsync(renameUserDto.UserId);
            var result = await cssUsersApiService.FindUserAsync("IDIR", renameUserDto.NewUserGuid);
            var cssUser = result.Data?.FirstOrDefault();

            if (cssUser == null || !SubjectsMatch(cssUser.Attributes?.IdirUserGuid?.FirstOrDefault(), person.OidcSub))
            {
                throw new UserFriendlyException("The selected directory user does not match the existing user subject.");
            }

            var username = cssUser.Attributes?.IdirUsername?.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new UserFriendlyException("The directory user does not have a username.");
            }

            var firstName = cssUser.FirstName ?? string.Empty;
            var lastName = cssUser.LastName ?? string.Empty;
            var email = cssUser.Email;
            var displayName = cssUser.Attributes?.DisplayName?.FirstOrDefault() ?? string.Empty;
            var identityUser = await identityUserRepository.GetAsync(renameUserDto.UserId);
            var fullName = $"{firstName} {lastName}";
            var badge = Utils.CreateUserBadge(identityUser);

            if (string.Equals(identityUser.UserName, username, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(identityUser.Name, firstName, StringComparison.Ordinal) &&
                string.Equals(identityUser.Surname, lastName, StringComparison.Ordinal) &&
                (string.IsNullOrWhiteSpace(email) || string.Equals(identityUser.Email, email, StringComparison.OrdinalIgnoreCase)) &&
                string.Equals(GetComparableDisplayName(person.OidcDisplayName), GetComparableDisplayName(displayName), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(person.FullName, fullName, StringComparison.Ordinal) &&
                string.Equals(person.Badge, badge, StringComparison.Ordinal))
            {
                return;
            }

            if (!string.Equals(identityUser.UserName, username, StringComparison.OrdinalIgnoreCase))
            {
                var usernameResult = await userManager.SetUserNameAsync(identityUser, username);
                if (!usernameResult.Succeeded)
                {
                    throw new UserFriendlyException("The directory username could not be applied to the user.");
                }
            }

            identityUser.Name = firstName;
            identityUser.Surname = lastName;
            if (!string.Equals(identityUser.Email, email, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(email))
            {
                await userManager.SetEmailAsync(identityUser, email);
                identityUser.SetEmailConfirmed(true);
            }

            identityUser.SetProperty("DisplayName", displayName);
            await identityUserRepository.UpdateAsync(identityUser, true);

            person.OidcDisplayName = displayName;
            person.FullName = fullName;
            person.Badge = Utils.CreateUserBadge(identityUser);
            await personRepository.UpdateAsync(person, true);
        }

        [Authorize(IdentityPermissions.Users.Update)]
        public async Task SetUserRolesAsync(Guid userId, string[] roleNames)
        {
            var user = await identityUserRepository.GetAsync(userId);
            await userManager.SetRolesAsync(user, roleNames);
        }

        private static bool SubjectsMatch(string? first, string? second)
        {
            return !string.IsNullOrWhiteSpace(first) &&
                !string.IsNullOrWhiteSpace(second) &&
                string.Equals(first.ToSubjectWithoutIdp(), second.ToSubjectWithoutIdp(), StringComparison.OrdinalIgnoreCase);
        }

        private static string GetComparableDisplayName(string? displayName)
        {
            return (displayName ?? string.Empty).Split(':')[0].Trim();
        }

        private async Task<IdentityUser?> CreateNewIdentityUserAsync(Guid newUserId, string? username, string? firstName, string? lastName, string? emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress)) emailAddress = null;
            IdentityUser? identityUser = new(newUserId, username, emailAddress ?? $"{Guid.Empty}@{username}", currentTenant.Id)
            {
                Name = firstName,
                Surname = lastName,
            };

            if (emailAddress != null)
            {
                identityUser.SetEmailConfirmed(true);
            }            

            // Use identity user manager to create the user
            var createUserResult = await userManager.CreateAsync(identityUser) ?? throw new AbpException("Unexpected error importing user");

            if (!createUserResult.Succeeded)
            {
                var validationErrors = new List<ValidationResult>();
                foreach (var error in createUserResult.Errors)
                {
                    validationErrors.Add(new ValidationResult($"{error.Code} {error.Description}"));
                }
                throw new AbpValidationException($"Error importing user id:{newUserId} u:{username} f:{firstName} l:{lastName} e:{emailAddress}", validationErrors);
            }

            return identityUser;
        }

        private async Task<IdentityUser?> ReactivateAndGetDeletedUserAsync(string username, string oidcSub, string? firstName, string? lastName)
        {
            //Temporary disable the ISoftDelete filter - find delete user account and reactivate for import
            using (dataFilter.Disable<ISoftDelete>())
            {
                var identityUser = await identityUserRepository
                    .FindByTenantIdAndUserNameAsync(username, currentTenant.Id);

                identityUser ??= (await userAccountsRepository.GetListByOidcSub(oidcSub))
                    .FirstOrDefault();

                if (identityUser != null)
                {
                    // Directory details (e.g. surname) can change between imports - keep the
                    // reactivated account's name/username current rather than leaving it stale.
                    if (!string.IsNullOrWhiteSpace(username) &&
                        !string.Equals(identityUser.UserName, username, StringComparison.OrdinalIgnoreCase))
                    {
                        await userManager.SetUserNameAsync(identityUser, username);
                    }

                    identityUser.Name = firstName ?? identityUser.Name;
                    identityUser.Surname = lastName ?? identityUser.Surname;
                    identityUser.IsDeleted = false;
                    identityUser.DeleterId = null;
                    identityUser.DeletionTime = null;
                    await identityUserRepository.UpdateAsync(identityUser);
                    return identityUser;
                }
            }

            return null;
        }

        private async Task SyncUserToCurrentTenantAsync(IdentityUser user, string oidcSub, string displayName)
        {
            var existingPerson = await personRepository.FindByOidcSub(oidcSub);
            if (existingPerson == null)
            {
                await personRepository.InsertAsync(new Person()
                {
                    Id = user.Id,
                    OidcSub = oidcSub.ToSubjectWithoutIdp(),
                    OidcDisplayName = displayName,
                    FullName = $"{user.Name} {user.Surname}",
                    Badge = Utils.CreateUserBadge(user)
                });
                return;
            }

            existingPerson.OidcDisplayName = displayName;
            existingPerson.FullName = $"{user.Name} {user.Surname}";
            existingPerson.Badge = Utils.CreateUserBadge(user);
            await personRepository.UpdateAsync(existingPerson);
        }

        private async Task<IdentityUser> UpdateAdditionalUserPropertiesAsync(IdentityUser user, string oidcSub, string displayName)
        {
            if (user != null)
            {
                user.SetProperty("OidcSub", oidcSub.ToSubjectWithoutIdp());
                user.SetProperty("DisplayName", displayName);
                await identityUserRepository.UpdateAsync(user, true);
            }

            return user!;
        }
    }
}
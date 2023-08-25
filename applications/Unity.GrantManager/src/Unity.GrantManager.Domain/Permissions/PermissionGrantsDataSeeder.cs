using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Identity;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.PermissionManagement;

namespace Unity.GrantManager.Permissions
{
    internal class PermissionGrantsDataSeeder : IDataSeedContributor, ITransientDependency
    {
        private readonly IPermissionDataSeeder _permissionDataSeeder;

        public PermissionGrantsDataSeeder(IPermissionDataSeeder permissionDataSeeder)
        {
            _permissionDataSeeder = permissionDataSeeder;
        }

        public async Task SeedAsync(DataSeedContext context)
        {            
            // Default permission grants based on role

            // - Program Manager
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.ProgramManager,
                new List<string>
                {
                    GrantManagerPermissions.Default,
                    GrantApplicationPermissions.Applications.Default,
                    GrantApplicationPermissions.Assignments.AssignInitial,
                    GrantApplicationPermissions.Reviews.StartInitial,
                    GrantApplicationPermissions.Reviews.CompleteInitial,
                    GrantApplicationPermissions.Adjudications.Start,
                    GrantApplicationPermissions.Adjudications.Complete,
                    GrantApplicationPermissions.Comments.Add
                });

            // - Reviewer
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.Reviewer,
                new List<string>
                {
                    GrantManagerPermissions.Default,
                    GrantApplicationPermissions.Applications.Default,
                    GrantApplicationPermissions.Reviews.StartInitial,
                    GrantApplicationPermissions.Reviews.CompleteInitial,
                    GrantApplicationPermissions.Comments.Add
                });
            //// TODO: manage organization profiles

            // - Adjudicator
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.Adjudicator,
               new List<string>
               {
                    GrantManagerPermissions.Default,
                    GrantApplicationPermissions.Applications.Default,
                    GrantApplicationPermissions.Reviews.StartInitial,
                    GrantApplicationPermissions.Reviews.CompleteInitial,
                    GrantApplicationPermissions.Adjudications.Start,
                    GrantApplicationPermissions.Comments.Add
               });

            // - TeamLead
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.TeamLead,
               new List<string>
               {
                    GrantManagerPermissions.Default,
                    GrantApplicationPermissions.Applications.Default,
                    GrantApplicationPermissions.Assignments.AssignInitial,
                    GrantApplicationPermissions.Reviews.StartInitial,
                    GrantApplicationPermissions.Reviews.CompleteInitial,
                    GrantApplicationPermissions.Adjudications.Start,
                    GrantApplicationPermissions.Adjudications.Complete,
                    GrantApplicationPermissions.Comments.Add
               });
            // TODO: manage organization profiles

            // - Approver
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.Approver,
              new List<string>
              {
                    GrantManagerPermissions.Default,
                    GrantApplicationPermissions.Applications.Default,
                    GrantApplicationPermissions.Approvals.Complete,
                    GrantApplicationPermissions.Comments.Add
              });

            // - BusinessAreaAdmin
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.BusinessAreaAdmin,
             new List<string>
             {
                    GrantManagerPermissions.Default,
                    GrantApplicationPermissions.Applications.Default,
                    GrantApplicationPermissions.Applicants.Edit,
                    GrantApplicationPermissions.Approvals.Complete,
                    GrantApplicationPermissions.Comments.Add,
                    IdentitySeedPermissions.Users.Default,
                    IdentitySeedPermissions.Users.Create,
                    IdentitySeedPermissions.Users.Update,
                    IdentitySeedPermissions.Users.Delete,
                    IdentitySeedPermissions.Users.ManagePermissions
             });

            // - SystemAdmin
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.SystemAdmin,
             new List<string>
             {
                    GrantManagerPermissions.Default,
                    SettingManagementSeedPermissions.Emailing,
                    SettingManagementSeedPermissions.EmailingTest
             });
        }
    }
}


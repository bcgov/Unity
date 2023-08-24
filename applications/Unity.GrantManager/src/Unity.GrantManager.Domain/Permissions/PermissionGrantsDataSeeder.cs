using System.Threading.Tasks;
using Unity.GrantManager.Identity;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.PermissionManagement;

namespace Unity.GrantManager.Permissions
{
    internal class PermissionGrantsDataSeeder : IDataSeedContributor, ITransientDependency
    {
        private readonly PermissionManager _permissionManager;

        public PermissionGrantsDataSeeder(PermissionManager permissionManager)
        {
            _permissionManager = permissionManager;
        }

        public async Task SeedAsync(DataSeedContext context)
        {
            // Default permission grants based on role

            // - Program Manager
            await _permissionManager.SetForRoleAsync(UnityRoles.ProgramManager, GrantManagerPermissions.Default, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.ProgramManager, GrantApplicationPermissions.Applications.Default, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.ProgramManager, GrantApplicationPermissions.Assignments.AssignInitial, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.ProgramManager, GrantApplicationPermissions.Reviews.StartInitial, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.ProgramManager, GrantApplicationPermissions.Reviews.CompleteInitial, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.ProgramManager, GrantApplicationPermissions.Adjudications.Start, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.ProgramManager, GrantApplicationPermissions.Adjudications.Complete, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.ProgramManager, GrantApplicationPermissions.Comments.Add, true);            
            // TODO: manage organization profiles

            // - Reviewer
            await _permissionManager.SetForRoleAsync(UnityRoles.Reviewer, GrantManagerPermissions.Default, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.Reviewer, GrantApplicationPermissions.Applications.Default, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.Reviewer, GrantApplicationPermissions.Reviews.StartInitial, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.Reviewer, GrantApplicationPermissions.Reviews.CompleteInitial, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.Reviewer, GrantApplicationPermissions.Comments.Add, true);

            // - Adjudicator
            await _permissionManager.SetForRoleAsync(UnityRoles.Adjudicator, GrantManagerPermissions.Default, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.Adjudicator, GrantApplicationPermissions.Applications.Default, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.Adjudicator, GrantApplicationPermissions.Reviews.StartInitial, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.Adjudicator, GrantApplicationPermissions.Reviews.CompleteInitial, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.Adjudicator, GrantApplicationPermissions.Adjudications.Start, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.Adjudicator, GrantApplicationPermissions.Comments.Add, true);

            // - TeamLead
            await _permissionManager.SetForRoleAsync(UnityRoles.TeamLead, GrantManagerPermissions.Default, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.TeamLead, GrantApplicationPermissions.Applications.Default, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.TeamLead, GrantApplicationPermissions.Assignments.AssignInitial, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.TeamLead, GrantApplicationPermissions.Reviews.StartInitial, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.TeamLead, GrantApplicationPermissions.Reviews.CompleteInitial, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.TeamLead, GrantApplicationPermissions.Adjudications.Start, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.TeamLead, GrantApplicationPermissions.Adjudications.Complete, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.TeamLead, GrantApplicationPermissions.Comments.Add, true);
            // TODO: manage organization profiles

            // - Approver
            await _permissionManager.SetForRoleAsync(UnityRoles.Approver, GrantManagerPermissions.Default, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.Approver, GrantApplicationPermissions.Applications.Default, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.Approver, GrantApplicationPermissions.Approvals.Complete, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.Approver, GrantApplicationPermissions.Comments.Add, true);

            // - BusinessAreaAdmin
            await _permissionManager.SetForRoleAsync(UnityRoles.BusinessAreaAdmin, GrantManagerPermissions.Default, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.BusinessAreaAdmin, GrantApplicationPermissions.Applications.Default, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.BusinessAreaAdmin, GrantApplicationPermissions.Applicants.Edit, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.BusinessAreaAdmin, GrantApplicationPermissions.Approvals.Complete, true);
            await _permissionManager.SetForRoleAsync(UnityRoles.BusinessAreaAdmin, GrantApplicationPermissions.Comments.Add, true);
            // TODO: user management

            // - SystemAdmin
            await _permissionManager.SetForRoleAsync(UnityRoles.SystemAdmin, GrantManagerPermissions.Default, true);
            // TODO: manage organization profiles, manage system settings
        }
    }
}
﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Identity;
using Unity.Payments.Permissions;
using Unity.Notifications.Permissions;
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
                    GrantApplicationPermissions.Comments.Add,
                    GrantManagerPermissions.Organizations.Default,
                    GrantManagerPermissions.Organizations.ManageProfiles,
                    IdentitySeedPermissions.Users.Default,
                    IdentitySeedPermissions.Users.Create,
                    IdentitySeedPermissions.Users.Update,
                    IdentitySeedPermissions.Users.Delete,
                    IdentitySeedPermissions.Users.ManagePermissions,
                    IdentitySeedPermissions.Roles.Default,
                    IdentitySeedPermissions.Roles.Create,
                    IdentitySeedPermissions.Roles.Update,
                    IdentitySeedPermissions.Roles.Delete,
                    IdentitySeedPermissions.Roles.ManagePermissions,
                    GrantManagerPermissions.Intakes.Default,
                    GrantManagerPermissions.ApplicationForms.Default,

                    GrantApplicationPermissions.AssessmentResults.Default,
                    GrantApplicationPermissions.AssessmentResults.Edit,
                    GrantApplicationPermissions.ApplicantInfo.Default,
                    GrantApplicationPermissions.ApplicantInfo.Update,
                    GrantApplicationPermissions.ProjectInfo.Default,
                    GrantApplicationPermissions.ProjectInfo.Update,
                    
                    NotificationsPermissions.Email.Default,
                    NotificationsPermissions.Email.Send,

                    //Dashboard 
                    GrantApplicationPermissions.Dashboard.Default,
                    GrantApplicationPermissions.Dashboard.ViewDashboard,
                    GrantApplicationPermissions.Dashboard.ApplicationStatusCount,
                    GrantApplicationPermissions.Dashboard.EconomicRegionCount,
                    GrantApplicationPermissions.Dashboard.ApplicationTagsCount,
                    GrantApplicationPermissions.Dashboard.ApplicationAssigneeCount,
                    GrantApplicationPermissions.Dashboard.RequestedAmountPerSubsector,
                    GrantApplicationPermissions.Dashboard.RequestApprovedCount,
                }, context.TenantId);

            // - Reviewer
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.Reviewer,
                new List<string>
                {
                    GrantManagerPermissions.Default,
                    GrantApplicationPermissions.Applications.Default,
                    GrantApplicationPermissions.Reviews.StartInitial,
                    GrantApplicationPermissions.Reviews.CompleteInitial,
                    GrantApplicationPermissions.Comments.Add,

                    // Assessments
                    GrantApplicationPermissions.Assessments.Default,
                    GrantApplicationPermissions.Assessments.Create,
                    GrantApplicationPermissions.Assessments.Confirm,

                    GrantApplicationPermissions.AssessmentResults.Default,
                    GrantApplicationPermissions.AssessmentResults.Edit,
                    GrantApplicationPermissions.ApplicantInfo.Default,
                    GrantApplicationPermissions.ApplicantInfo.Update,
                    GrantApplicationPermissions.ProjectInfo.Default,
                    GrantApplicationPermissions.ProjectInfo.Update,
                    
                    NotificationsPermissions.Email.Default,
                    NotificationsPermissions.Email.Send,

                    //Dashboard 
                    GrantApplicationPermissions.Dashboard.Default,
                    GrantApplicationPermissions.Dashboard.ViewDashboard,
                    GrantApplicationPermissions.Dashboard.ApplicationStatusCount,
                    GrantApplicationPermissions.Dashboard.EconomicRegionCount,
                    GrantApplicationPermissions.Dashboard.ApplicationTagsCount,
                    GrantApplicationPermissions.Dashboard.ApplicationAssigneeCount,
                    GrantApplicationPermissions.Dashboard.RequestedAmountPerSubsector,
                    GrantApplicationPermissions.Dashboard.RequestApprovedCount,
                }, context.TenantId);

            // - Assessor
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.Assessor,
                new List<string>
                {
                    GrantManagerPermissions.Default,
                    GrantApplicationPermissions.Applications.Default,
                    GrantApplicationPermissions.Reviews.StartInitial,
                    GrantApplicationPermissions.Reviews.CompleteInitial,
                    GrantApplicationPermissions.Comments.Add,

                    // Assessments
                    GrantApplicationPermissions.Assessments.Default,
                    GrantApplicationPermissions.Assessments.Create,
                    GrantApplicationPermissions.Assessments.Confirm,

                    GrantApplicationPermissions.AssessmentResults.Default,
                    GrantApplicationPermissions.AssessmentResults.Edit,
                    GrantApplicationPermissions.ApplicantInfo.Default,
                    GrantApplicationPermissions.ApplicantInfo.Update,
                    GrantApplicationPermissions.ProjectInfo.Default,
                    GrantApplicationPermissions.ProjectInfo.Update,

                    NotificationsPermissions.Email.Default,
                    NotificationsPermissions.Email.Send,

                    //Dashboard 
                    GrantApplicationPermissions.Dashboard.Default,
                    GrantApplicationPermissions.Dashboard.ViewDashboard,
                    GrantApplicationPermissions.Dashboard.ApplicationStatusCount,
                    GrantApplicationPermissions.Dashboard.EconomicRegionCount,
                    GrantApplicationPermissions.Dashboard.ApplicationTagsCount,
                    GrantApplicationPermissions.Dashboard.ApplicationAssigneeCount,
                    GrantApplicationPermissions.Dashboard.RequestedAmountPerSubsector,
                    GrantApplicationPermissions.Dashboard.RequestApprovedCount,
                }, context.TenantId);

            // - TeamLead
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.TeamLead,
                new List<string>
                {
                    GrantManagerPermissions.Default,
                    GrantApplicationPermissions.Applications.Default,
                    GrantApplicationPermissions.Assignments.AssignInitial,
                    GrantApplicationPermissions.Reviews.StartInitial,
                    GrantApplicationPermissions.Reviews.CompleteInitial,
                    GrantApplicationPermissions.Comments.Add,
                    GrantManagerPermissions.Organizations.Default,
                    GrantManagerPermissions.Organizations.ManageProfiles,
                    
                    // Assessments
                    GrantApplicationPermissions.Assessments.Default,
                    GrantApplicationPermissions.Assessments.Create,
                    GrantApplicationPermissions.Assessments.SendBack,
                    GrantApplicationPermissions.Assessments.Confirm,

                    GrantApplicationPermissions.AssessmentResults.Default,
                    GrantApplicationPermissions.AssessmentResults.Edit,
                    GrantApplicationPermissions.ApplicantInfo.Default,
                    GrantApplicationPermissions.ApplicantInfo.Update,
                    GrantApplicationPermissions.ProjectInfo.Default,
                    GrantApplicationPermissions.ProjectInfo.Update,

                    NotificationsPermissions.Email.Default,
                    NotificationsPermissions.Email.Send,

                    //Dashboard 
                    GrantApplicationPermissions.Dashboard.Default,
                    GrantApplicationPermissions.Dashboard.ViewDashboard,
                    GrantApplicationPermissions.Dashboard.ApplicationStatusCount,
                    GrantApplicationPermissions.Dashboard.EconomicRegionCount,
                    GrantApplicationPermissions.Dashboard.ApplicationTagsCount,
                    GrantApplicationPermissions.Dashboard.ApplicationAssigneeCount,
                    GrantApplicationPermissions.Dashboard.RequestedAmountPerSubsector,
                    GrantApplicationPermissions.Dashboard.RequestApprovedCount,
                }, context.TenantId);

            // - Approver
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.Approver,
                new List<string>
                {
                    GrantManagerPermissions.Default,
                    GrantApplicationPermissions.Applications.Default,
                    GrantApplicationPermissions.Approvals.Complete,
                    GrantApplicationPermissions.Comments.Add,

                    GrantApplicationPermissions.AssessmentResults.Default,
                    GrantApplicationPermissions.AssessmentResults.Edit,
                    GrantApplicationPermissions.AssessmentResults.EditFinalStateFields,
                    GrantApplicationPermissions.ApplicantInfo.Default,
                    GrantApplicationPermissions.ApplicantInfo.Update,
                    GrantApplicationPermissions.ProjectInfo.Default,
                    GrantApplicationPermissions.ProjectInfo.Update,
                    GrantApplicationPermissions.ProjectInfo.UpdateFinalStateFields,

                    NotificationsPermissions.Email.Default,
                    NotificationsPermissions.Email.Send,

                    //Dashboard 
                    GrantApplicationPermissions.Dashboard.Default,
                    GrantApplicationPermissions.Dashboard.ViewDashboard,
                    GrantApplicationPermissions.Dashboard.ApplicationStatusCount,
                    GrantApplicationPermissions.Dashboard.EconomicRegionCount,
                    GrantApplicationPermissions.Dashboard.ApplicationTagsCount,
                    GrantApplicationPermissions.Dashboard.ApplicationAssigneeCount,
                    GrantApplicationPermissions.Dashboard.RequestedAmountPerSubsector,
                    GrantApplicationPermissions.Dashboard.RequestApprovedCount,
                }, context.TenantId);

            // - SystemAdmin
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.SystemAdmin,
                new List<string>
                {
                    GrantManagerPermissions.Default,
                    UnitySettingManagementPermissions.UserInterface,
                    GrantManagerPermissions.Organizations.Default,
                    GrantManagerPermissions.Organizations.ManageProfiles,
                    GrantManagerPermissions.Intakes.Default,
                    GrantManagerPermissions.ApplicationForms.Default,

                    //Dashboard 
                    GrantApplicationPermissions.Dashboard.Default,
                    GrantApplicationPermissions.Dashboard.ViewDashboard,
                    GrantApplicationPermissions.Dashboard.ApplicationStatusCount,
                    GrantApplicationPermissions.Dashboard.EconomicRegionCount,
                    GrantApplicationPermissions.Dashboard.ApplicationTagsCount,
                    GrantApplicationPermissions.Dashboard.ApplicationAssigneeCount,
                    GrantApplicationPermissions.Dashboard.RequestedAmountPerSubsector,
                    GrantApplicationPermissions.Dashboard.RequestApprovedCount,

                    // Assessments
                    GrantApplicationPermissions.Assessments.Default,
                    GrantApplicationPermissions.Assessments.Create,
                    GrantApplicationPermissions.Assessments.SendBack,
                    GrantApplicationPermissions.Assessments.Confirm,

                    GrantApplicationPermissions.AssessmentResults.Default,
                    GrantApplicationPermissions.AssessmentResults.Edit,
                    GrantApplicationPermissions.ApplicantInfo.Default,
                    GrantApplicationPermissions.ApplicantInfo.Update,
                    GrantApplicationPermissions.ProjectInfo.Default,
                    GrantApplicationPermissions.ProjectInfo.Update,

                    NotificationsPermissions.Email.Default,
                    NotificationsPermissions.Email.Send,
                    NotificationsPermissions.Settings,
                    
                    UnitySettingManagementPermissions.BackgroundJobSettings
                }, context.TenantId);


            // -L1 Approver
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.L1Approver,
                new List<string>
                {
                    GrantManagerPermissions.Default,
                    GrantApplicationPermissions.Applications.Default,
                    PaymentsPermissions.Payments.Default,
                    PaymentsPermissions.Payments.L1ApproveOrDecline,

                    GrantApplicationPermissions.AssessmentResults.Default,
                    GrantApplicationPermissions.AssessmentResults.Edit,
                    GrantApplicationPermissions.ApplicantInfo.Default,
                    GrantApplicationPermissions.ApplicantInfo.Update,
                    GrantApplicationPermissions.ProjectInfo.Default,
                    GrantApplicationPermissions.ProjectInfo.Update,

                    NotificationsPermissions.Email.Default,
                    NotificationsPermissions.Email.Send,

                    //Dashboard 
                    GrantApplicationPermissions.Dashboard.Default,
                    GrantApplicationPermissions.Dashboard.ViewDashboard,
                    GrantApplicationPermissions.Dashboard.ApplicationStatusCount,
                    GrantApplicationPermissions.Dashboard.EconomicRegionCount,
                    GrantApplicationPermissions.Dashboard.ApplicationTagsCount,
                    GrantApplicationPermissions.Dashboard.ApplicationAssigneeCount,
                    GrantApplicationPermissions.Dashboard.RequestedAmountPerSubsector,
                    GrantApplicationPermissions.Dashboard.RequestApprovedCount,
                }, context.TenantId);

            // -L2 Approver
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.L2Approver,
                new List<string>
                {
                    GrantManagerPermissions.Default,
                    GrantApplicationPermissions.Applications.Default,
                    PaymentsPermissions.Payments.Default,
                    PaymentsPermissions.Payments.L2ApproveOrDecline,

                    GrantApplicationPermissions.AssessmentResults.Default,
                    GrantApplicationPermissions.AssessmentResults.Edit,
                    GrantApplicationPermissions.ApplicantInfo.Default,
                    GrantApplicationPermissions.ApplicantInfo.Update,
                    GrantApplicationPermissions.ProjectInfo.Default,
                    GrantApplicationPermissions.ProjectInfo.Update,

                    NotificationsPermissions.Email.Default,
                    NotificationsPermissions.Email.Send,

                    //Dashboard 
                    GrantApplicationPermissions.Dashboard.Default,
                    GrantApplicationPermissions.Dashboard.ViewDashboard,
                    GrantApplicationPermissions.Dashboard.ApplicationStatusCount,
                    GrantApplicationPermissions.Dashboard.EconomicRegionCount,
                    GrantApplicationPermissions.Dashboard.ApplicationTagsCount,
                    GrantApplicationPermissions.Dashboard.ApplicationAssigneeCount,
                    GrantApplicationPermissions.Dashboard.RequestedAmountPerSubsector,
                    GrantApplicationPermissions.Dashboard.RequestApprovedCount,
                }, context.TenantId);

            // -L3 Approver
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.L3Approver,
                new List<string>
                {
                    GrantManagerPermissions.Default,
                    GrantApplicationPermissions.Applications.Default,
                    PaymentsPermissions.Payments.Default,
                    PaymentsPermissions.Payments.L3ApproveOrDecline,

                    GrantApplicationPermissions.AssessmentResults.Default,
                    GrantApplicationPermissions.AssessmentResults.Edit,
                    GrantApplicationPermissions.ApplicantInfo.Default,
                    GrantApplicationPermissions.ApplicantInfo.Update,
                    GrantApplicationPermissions.ProjectInfo.Default,
                    GrantApplicationPermissions.ProjectInfo.Update,

                    NotificationsPermissions.Email.Default,
                    NotificationsPermissions.Email.Send,

                    //Dashboard 
                    GrantApplicationPermissions.Dashboard.Default,
                    GrantApplicationPermissions.Dashboard.ViewDashboard,
                    GrantApplicationPermissions.Dashboard.ApplicationStatusCount,
                    GrantApplicationPermissions.Dashboard.EconomicRegionCount,
                    GrantApplicationPermissions.Dashboard.ApplicationTagsCount,
                    GrantApplicationPermissions.Dashboard.ApplicationAssigneeCount,
                    GrantApplicationPermissions.Dashboard.RequestedAmountPerSubsector,
                    GrantApplicationPermissions.Dashboard.RequestApprovedCount,
                }, context.TenantId);

            // -External Assessor
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.ExternalAssessor,
                new List<string>
                {
                    GrantManagerPermissions.Default,
                    GrantApplicationPermissions.Applications.Default,
                    PaymentsPermissions.Payments.Default,

                    GrantApplicationPermissions.AssessmentResults.Default,
                    GrantApplicationPermissions.ApplicantInfo.Default,
                    GrantApplicationPermissions.ProjectInfo.Default,

                    NotificationsPermissions.Email.Default,
                }, context.TenantId);

        }
    }
}


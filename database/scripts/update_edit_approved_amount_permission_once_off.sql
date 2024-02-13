UPDATE "Permissions"
SET "Name" = 'GrantApplicationManagement.AssessmentResults.EditFinalStateFields', "DisplayName" = 'L:GrantManager,Permission:GrantApplicationPermissions.AssessmentResults.EditFinalStateFields'
WHERE "Name" = 'GrantApplicationManagement.AssessmentResults.EditApprovedAmount'

UPDATE "PermissionGrants"
SET "Name" = 'GrantApplicationManagement.AssessmentResults.EditFinalStateFields'
WHERE "Name" = 'GrantApplicationManagement.AssessmentResults.EditApprovedAmount';
using Microsoft.EntityFrameworkCore;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Domain.Shared;
using Unity.Payments.Domain.Workflow;
using Unity.Payments.Enums;
using Unity.Payments.PaymentRequests;
using Unity.Payments.Permissions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Domain.Services;
using Volo.Abp.Uow;
using Volo.Abp.Users;

namespace Unity.Payments.Domain.Services
{
    public class PaymentsManager(
            IApplicationRepository applicationRepository,
            IApplicationFormRepository applicationFormRepository,
            CasPaymentRequestCoordinator casPaymentRequestCoordinator,
            IPaymentRequestRepository paymentRequestRepository,
            IUnitOfWorkManager unitOfWorkManager,
            IPermissionChecker permissionChecker,
            ICurrentUser currentUser) : DomainService, IPaymentsManager
    {

        private void ConfigureWorkflow(StateMachine<PaymentRequestStatus, PaymentApprovalAction> paymentStateMachine)
        {
            paymentStateMachine.Configure(PaymentRequestStatus.L1Pending)
                .PermitIf(PaymentApprovalAction.L1Approve, PaymentRequestStatus.L2Pending, () => HasPermission(PaymentsPermissions.Payments.L1ApproveOrDecline))
                .PermitIf(PaymentApprovalAction.L1Decline, PaymentRequestStatus.L1Declined, () => HasPermission(PaymentsPermissions.Payments.L1ApproveOrDecline));

            paymentStateMachine.Configure(PaymentRequestStatus.L1Declined)
                  .PermitIf(PaymentApprovalAction.L1Approve, PaymentRequestStatus.L2Pending, () => HasPermission(PaymentsPermissions.Payments.L1ApproveOrDecline));

            paymentStateMachine.Configure(PaymentRequestStatus.L2Pending)
                .PermitIf(PaymentApprovalAction.L2Approve, PaymentRequestStatus.L3Pending, () => HasPermission(PaymentsPermissions.Payments.L2ApproveOrDecline))
                .PermitIf(PaymentApprovalAction.Submit, PaymentRequestStatus.Submitted, () => HasPermission(PaymentsPermissions.Payments.L2ApproveOrDecline))
                .PermitIf(PaymentApprovalAction.L2Decline, PaymentRequestStatus.L2Declined, () => HasPermission(PaymentsPermissions.Payments.L2ApproveOrDecline));

            paymentStateMachine.Configure(PaymentRequestStatus.L2Declined)
                .PermitIf(PaymentApprovalAction.L2Approve, PaymentRequestStatus.L3Pending, () => HasPermission(PaymentsPermissions.Payments.L2ApproveOrDecline))
                .PermitIf(PaymentApprovalAction.Submit, PaymentRequestStatus.Submitted, () => HasPermission(PaymentsPermissions.Payments.L2ApproveOrDecline));

            paymentStateMachine.Configure(PaymentRequestStatus.L3Pending)
                .PermitIf(PaymentApprovalAction.Submit, PaymentRequestStatus.Submitted, () => HasPermission(PaymentsPermissions.Payments.L3ApproveOrDecline))
                .PermitIf(PaymentApprovalAction.L3Decline, PaymentRequestStatus.L3Declined, () => HasPermission(PaymentsPermissions.Payments.L3ApproveOrDecline));

            paymentStateMachine.Configure(PaymentRequestStatus.L2Declined)
                .PermitIf(PaymentApprovalAction.Submit, PaymentRequestStatus.Submitted, () => HasPermission(PaymentsPermissions.Payments.L2ApproveOrDecline));
        }

        private bool HasPermission(string permission)
        {
            return permissionChecker.IsGrantedAsync(permission).Result;
        }

        public async Task<List<PaymentActionResultItem>> GetActions(Guid paymentRequestsId)
        {
            var paymentRequest = await paymentRequestRepository.GetAsync(paymentRequestsId, true);

            var Workflow = new PaymentsWorkflow<PaymentRequestStatus, PaymentApprovalAction>(
                () => paymentRequest.Status,
                s => paymentRequest.SetPaymentRequestStatus(s), ConfigureWorkflow);

            var allActions = Workflow.GetAllActions().Distinct().ToList();
            var permittedActions = Workflow.GetPermittedActions().ToList();

            var actionsList = allActions
                .Select(trigger =>
                new PaymentActionResultItem
                {
                    PaymentApprovalAction = trigger,
                    IsPermitted = permittedActions.Contains(trigger),
                    IsInternal = trigger.ToString().StartsWith("Internal_")
                })
                .OrderBy(x => (int)x.PaymentApprovalAction)
                .ToList();

            return actionsList;
        }

        public async Task<PaymentRequest> TriggerAction(Guid paymentRequestsId, PaymentApprovalAction triggerAction)
        {
            var paymentRequest = await paymentRequestRepository.GetAsync(paymentRequestsId, true);
            var currentUserId = currentUser.GetId();

            var statusChange = paymentRequest.Status;

            var Workflow = new PaymentsWorkflow<PaymentRequestStatus, PaymentApprovalAction>(
                () => statusChange,
                s => statusChange = s,
            ConfigureWorkflow);

            await Workflow.ExecuteActionAsync(triggerAction);

            var statusChangedTo = PaymentRequestStatus.L1Pending;

            if (triggerAction == PaymentApprovalAction.L1Approve)
            {
                var index = paymentRequest.ExpenseApprovals.FindIndex(i => i.Type == ExpenseApprovalType.Level1);
                paymentRequest.ExpenseApprovals[index].Approve(currentUserId);
                statusChangedTo = PaymentRequestStatus.L2Pending;
            }
            else if (triggerAction == PaymentApprovalAction.L1Decline)
            {
                var index = paymentRequest.ExpenseApprovals.FindIndex(i => i.Type == ExpenseApprovalType.Level1);
                paymentRequest.ExpenseApprovals[index].Decline(currentUserId);
                statusChangedTo = PaymentRequestStatus.L1Declined;
            }
            else if (triggerAction == PaymentApprovalAction.L2Approve)
            {
                var index = paymentRequest.ExpenseApprovals.FindIndex(i => i.Type == ExpenseApprovalType.Level2);
                paymentRequest.ExpenseApprovals[index].Approve(currentUserId);
                statusChangedTo = PaymentRequestStatus.L3Pending;

            }
            else if (triggerAction == PaymentApprovalAction.L2Decline)
            {
                var index = paymentRequest.ExpenseApprovals.FindIndex(i => i.Type == ExpenseApprovalType.Level2);
                paymentRequest.ExpenseApprovals[index].Decline(currentUserId);
                statusChangedTo = PaymentRequestStatus.L2Declined;
            }

            else if (triggerAction == PaymentApprovalAction.L3Decline)
            {
                var index = paymentRequest.ExpenseApprovals.FindIndex(i => i.Type == ExpenseApprovalType.Level3);
                paymentRequest.ExpenseApprovals[index].Decline(currentUserId);
                statusChangedTo = PaymentRequestStatus.L3Declined;
            }

            else if (triggerAction == PaymentApprovalAction.Submit)
            {
                if (HasPermission(PaymentsPermissions.Payments.L2ApproveOrDecline))
                {
                    var index = paymentRequest.ExpenseApprovals.FindIndex(i => i.Type == ExpenseApprovalType.Level2);
                    paymentRequest.ExpenseApprovals[index].Approve(currentUserId);
                }
                else if (HasPermission(PaymentsPermissions.Payments.L3ApproveOrDecline))
                {
                    var index = paymentRequest.ExpenseApprovals.FindIndex(i => i.Type == ExpenseApprovalType.Level3);
                    paymentRequest.ExpenseApprovals[index].Approve(currentUserId);
                }
                bool preventPayment = await GetFormPreventPaymentStatusByApplicationId(paymentRequest.CorrelationId);

                if (preventPayment)
                {
                    statusChangedTo = PaymentRequestStatus.FSB;
                }
                else
                {
                    statusChangedTo = PaymentRequestStatus.Submitted;
                    await casPaymentRequestCoordinator.AddPaymentRequestsToInvoiceQueue(paymentRequest);
                }
                
                
            }
            paymentRequest.SetPaymentRequestStatus(statusChangedTo);

            return await paymentRequestRepository.UpdateAsync(paymentRequest);
        }

        public async Task<bool> GetFormPreventPaymentStatusByPaymentRequestId(Guid paymentRequestId)
        {
            PaymentRequest paymentRequest = await paymentRequestRepository.GetAsync(paymentRequestId);
            Guid applicationId = paymentRequest.CorrelationId;
            var applicationQueryable = await applicationRepository.GetQueryableAsync();
            var applicationWithIncludes = await applicationQueryable.Where(a => a.Id == applicationId)
                .Include(a => a.ApplicationForm).ToListAsync();

            var appForm = applicationWithIncludes.FirstOrDefault()?.ApplicationForm;
            return appForm != null && appForm.PreventPayment;
        }
        
        public async Task<bool> GetFormPreventPaymentStatusByApplicationId(Guid applicationId)
        {
            Application application = await applicationRepository.GetAsync(applicationId);
            Guid formId = application.ApplicationForm.Id;
            ApplicationForm appForm = await applicationFormRepository.GetAsync(formId);
            return appForm.PreventPayment;
        }        

        public async Task UpdatePaymentStatusAsync(Guid paymentRequestId, PaymentApprovalAction triggerAction)
        {
            using var uow = unitOfWorkManager.Begin();

            await TriggerAction(paymentRequestId, triggerAction);

            await uow.SaveChangesAsync();
        }
    }
}

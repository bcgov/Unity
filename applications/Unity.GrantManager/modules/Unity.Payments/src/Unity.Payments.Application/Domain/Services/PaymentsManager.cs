using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Domain.Shared;
using Unity.Payments.Domain.Workflow;
using Unity.Payments.Enums;
using Unity.Payments.Integrations.Cas;
using Unity.Payments.PaymentRequests;
using Unity.Payments.Permissions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Domain.Services;
using Volo.Abp.Uow;

namespace Unity.Payments.Domain.Services
{
    public class PaymentsManager : DomainService, IPaymentsManager
    {
        /* To be implemented */
        private readonly IPaymentRequestRepository _paymentRequestRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IPermissionChecker _permissionChecker;
        private readonly CasPaymentRequestCoordinator _casPaymentRequestCoordinator;

        public PaymentsManager(
            CasPaymentRequestCoordinator casPaymentRequestCoordinator,
            IInvoiceService invoiceService,
            IPaymentRequestRepository paymentRequestRepository,
            IUnitOfWorkManager unitOfWorkManager,
            IPermissionChecker permissionChecker)
        {
            _casPaymentRequestCoordinator = casPaymentRequestCoordinator;
            _paymentRequestRepository = paymentRequestRepository;
            _unitOfWorkManager = unitOfWorkManager;
            _permissionChecker = permissionChecker;
        }

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
            return _permissionChecker.IsGrantedAsync(permission).Result;
        }


        public async Task<List<PaymentActionResultItem>> GetActions(Guid paymentRequestsId)
        {
            var paymentRequest = await _paymentRequestRepository.GetAsync(paymentRequestsId, true);

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
            var paymentRequest = await _paymentRequestRepository.GetAsync(paymentRequestsId, true);

            var statusChange = paymentRequest.Status;

            var Workflow = new PaymentsWorkflow<PaymentRequestStatus, PaymentApprovalAction>(
                () => statusChange,
                s => statusChange = s,
            ConfigureWorkflow);

            await Workflow.ExecuteActionAsync(triggerAction);

            var statusChangedTo = PaymentRequestStatus.L1Pending;

            if (triggerAction == PaymentApprovalAction.L1Approve)
            {
                var index = paymentRequest.ExpenseApprovals.FindIndex(i => i.Type == Enums.ExpenseApprovalType.Level1);
                paymentRequest.ExpenseApprovals[index].Approve();
                statusChangedTo = PaymentRequestStatus.L2Pending;
            }
            else if (triggerAction == PaymentApprovalAction.L1Decline)
            {
                var index = paymentRequest.ExpenseApprovals.FindIndex(i => i.Type == Enums.ExpenseApprovalType.Level1);
                paymentRequest.ExpenseApprovals[index].Decline();
                statusChangedTo = PaymentRequestStatus.L1Declined;
            }
            else if (triggerAction == PaymentApprovalAction.L2Approve)
            {
                var index = paymentRequest.ExpenseApprovals.FindIndex(i => i.Type == Enums.ExpenseApprovalType.Level2);
                paymentRequest.ExpenseApprovals[index].Approve();
                statusChangedTo = PaymentRequestStatus.L3Pending;

            }
            else if (triggerAction == PaymentApprovalAction.L2Decline)
            {
                var index = paymentRequest.ExpenseApprovals.FindIndex(i => i.Type == Enums.ExpenseApprovalType.Level2);
                paymentRequest.ExpenseApprovals[index].Decline();
                statusChangedTo = PaymentRequestStatus.L2Declined;
            }

            else if (triggerAction == PaymentApprovalAction.L3Decline)
            {
                var index = paymentRequest.ExpenseApprovals.FindIndex(i => i.Type == Enums.ExpenseApprovalType.Level3);
                paymentRequest.ExpenseApprovals[index].Decline();
                statusChangedTo = PaymentRequestStatus.L3Declined;
            }

            else if (triggerAction == PaymentApprovalAction.Submit)
            {
                if (HasPermission(PaymentsPermissions.Payments.L2ApproveOrDecline))
                {
                    var index = paymentRequest.ExpenseApprovals.FindIndex(i => i.Type == Enums.ExpenseApprovalType.Level2);
                    paymentRequest.ExpenseApprovals[index].Approve();
                }
                else if (HasPermission(PaymentsPermissions.Payments.L3ApproveOrDecline))
                {
                    var index = paymentRequest.ExpenseApprovals.FindIndex(i => i.Type == Enums.ExpenseApprovalType.Level3);
                    paymentRequest.ExpenseApprovals[index].Approve();
                }

                statusChangedTo = PaymentRequestStatus.Submitted;
                await _casPaymentRequestCoordinator.AddPaymentRequestsToInvoiceQueue(paymentRequest);
            }
            paymentRequest.SetPaymentRequestStatus(statusChangedTo);

            return await _paymentRequestRepository.UpdateAsync(paymentRequest);
        }

        public async Task UpdatePaymentStatusAsync(Guid paymentRequestId, PaymentApprovalAction triggerAction)
        {
            using var uow = _unitOfWorkManager.Begin();

            await TriggerAction(paymentRequestId, triggerAction);

            await uow.SaveChangesAsync();
        }
    }
}

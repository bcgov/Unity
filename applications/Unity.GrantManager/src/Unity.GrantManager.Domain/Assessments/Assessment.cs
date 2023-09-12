using Stateless;
using System;
using System.Collections.Generic;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Assessments
{
    public class Assessment : AuditedAggregateRoot<Guid>
    {
        public Guid ApplicationId { get; set; }
        public Guid AssignedUserId { get; private set; }

        public DateTime? EndDate { get; set; }

        public bool IsComplete { get; set; }

        public bool? ApprovalRecommended { get; set; }

        public AssessmentState Status { get; private set; }
        private readonly StateMachine<AssessmentState, AssessmentAction> _workflow;

        private Assessment()
        {
            /* This constructor is for deserialization / ORM purpose */
            _workflow = new StateMachine<AssessmentState, AssessmentAction>(AssessmentState.IN_PROGRESS);
        }

        public Assessment(
            Guid id,
            Guid applicationId,
            Guid assignedUserId,
            AssessmentState status = AssessmentState.IN_PROGRESS)
            : base(id)
        {
            ApplicationId = applicationId;
            AssignedUserId = assignedUserId;
            Status = status;

            _workflow = new StateMachine<AssessmentState, AssessmentAction>(
                () => Status, 
                s => Status = s);

            ConfigureWorkflow();
        }

        private void ConfigureWorkflow()
        {
            _workflow.Configure(AssessmentState.IN_PROGRESS)
                .Permit(AssessmentAction.SendToTeamLead, AssessmentState.IN_REVIEW);

            _workflow.Configure(AssessmentState.IN_REVIEW)
                .Permit(AssessmentAction.SendBack, AssessmentState.IN_PROGRESS)
                .Permit(AssessmentAction.Confirm, AssessmentState.COMPLETED);

            _workflow.Configure(AssessmentState.COMPLETED)
                .OnEntry(OnResolved)
                .OnExit(OnReopened);
        }

        private void OnResolved()
        {
            EndDate = DateTime.UtcNow;
        }

        private void OnReopened()
        {
            EndDate = null;
        }

        /// <summary>
        /// The collection of available <see cref="AssessmentAction" />s for an
        /// <see cref="Assessment"/> given its current <see cref="Status" />.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<AssessmentAction> GetActions()
        {
            return _workflow.GetPermittedTriggers();
        }

        /// <summary>
        /// The current <see cref="AssessmentState" /> of the <see cref="Assessment"/>.
        /// </summary>
        public AssessmentState GetWorkflowState()
        {
            if (_workflow is not null)
            {
                return _workflow.State;
            }
            else
            {
                throw new InvalidOperationException($"{typeof(Assessment)} hasn't been configured yet.");
            }
        }

        public Assessment ExecuteAction(AssessmentAction action)
        {
            if (_workflow.CanFire(action))
            {
                _workflow.Fire(action);
            }
            else
            {
                throw new BusinessException($"Cannot transition from {GetWorkflowState} via {action}");
            }

            return this;
        }
    }
}

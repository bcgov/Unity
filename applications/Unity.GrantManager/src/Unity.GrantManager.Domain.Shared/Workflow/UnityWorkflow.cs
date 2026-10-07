using Stateless;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;

namespace Unity.GrantManager.Workflow;
public class UnityWorkflow<TStates, TTriggers>
{
    internal StateMachine<TStates, TTriggers> _stateMachine;

    public UnityWorkflow(
        Func<TStates> stateAccessor,
        Action<TStates> stateMutator,
        Action<StateMachine<TStates, TTriggers>> configurationDelegateMethod)
    {
        _stateMachine = new StateMachine<TStates, TTriggers>(
            stateAccessor,
            stateMutator);

        Configure(configurationDelegateMethod);
    }

    protected void Configure(Action<StateMachine<TStates, TTriggers>> configurationDelegateMethod)
    {
        ArgumentNullException.ThrowIfNull(configurationDelegateMethod);
        configurationDelegateMethod(_stateMachine);
    }

    public virtual TStates GetState()
    {
        if (_stateMachine is not null)
        {
            return _stateMachine.State;
        }
        else
        {
            throw new InvalidOperationException($"The state machine hasn't been configured yet.");
        }
    }

    public virtual async Task ExecuteActionAsync(TTriggers action)
    {
        var permittedTriggers = await _stateMachine.GetPermittedTriggersAsync();
        
        if (permittedTriggers.Contains(action))
        {
            await _stateMachine.FireAsync(action);
        }
        else
        {
            // Idempotent behavior: silently allow if already in target state (e.g., completing an already-completed assessment)
            // This prevents batch-complete operations from failing when duplicate items are selected
            // Only throw if this is clearly an invalid transition (not handled by state machine)
            try
            {
                await _stateMachine.FireAsync(action);
            }
            catch (InvalidOperationException)
            {
                // State machine rejected the transition - this is expected for invalid states
                throw new BusinessException("InvalidStateTransition",
                    $"Cannot transition from {_stateMachine.State} via {action}");
            }
        }
    }
}

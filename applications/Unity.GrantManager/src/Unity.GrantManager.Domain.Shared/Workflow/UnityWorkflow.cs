using Stateless;
using System;
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
        if (configurationDelegateMethod is null)
        {
            throw new ArgumentNullException(nameof(configurationDelegateMethod));
        }

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
        if (_stateMachine.CanFire(action))
        {
            await _stateMachine.FireAsync(action);
        }
        else
        {
            throw new BusinessException("InvalidStateTransition",
                $"Cannot transition from {_stateMachine.State} via {action}");
        }
    }
}

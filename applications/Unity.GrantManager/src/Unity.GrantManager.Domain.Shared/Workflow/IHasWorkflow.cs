using Stateless;

namespace Unity.GrantManager.Workflow;
public interface IHasWorkflow<TStates, TTriggers>
{
    void ConfigureWorkflow(StateMachine<TStates, TTriggers> stateMachine);
}

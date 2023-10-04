using Stateless.Graph;
using System.Collections.Generic;
using System.Linq;

namespace Unity.GrantManager.Workflow;
public static class UnityWorkflowExtensions
{
    public static IEnumerable<TTriggers> GetPermittedActions<TStates, TTriggers>(this UnityWorkflow<TStates, TTriggers> workflow)
    {
        return workflow._stateMachine.GetPermittedTriggers();
    }

    public static IEnumerable<TTriggers> GetAllActions<TStates, TTriggers>(this UnityWorkflow<TStates, TTriggers> workflow)
    {
        var workflowInfo = workflow._stateMachine.GetInfo();
        return workflowInfo.States
            .SelectMany(x => x.Transitions)
            .Select(x => (TTriggers)x.Trigger.UnderlyingTrigger);
    }

    public static string? GetWorkflowDiagram<TStates, TTriggers>(this UnityWorkflow<TStates, TTriggers> workflow)
    {
        return UmlDotGraph.Format(workflow._stateMachine.GetInfo());
    }
}

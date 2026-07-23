using Stateless.Graph;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Unity.GrantManager.Workflow;
public static class UnityWorkflowExtensions
{
    /// <summary>
    /// The currently permitted actions allowed by the workflow state machine.
    /// </summary>
    public static async Task<IEnumerable<TTriggers>> GetPermittedActions<TStates, TTriggers>(this UnityWorkflow<TStates, TTriggers> workflow)
    {
        return await workflow._stateMachine.GetPermittedTriggersAsync();
    }

    /// <summary>
    /// All actions configured with the workflow state machine.
    /// </summary>
    public static IEnumerable<TTriggers> GetAllActions<TStates, TTriggers>(this UnityWorkflow<TStates, TTriggers> workflow)
    {
        var workflowInfo = workflow._stateMachine.GetInfo();
        return workflowInfo.States
            .SelectMany(x => x.Transitions)
            .Select(x => (TTriggers)x.Trigger.UnderlyingTrigger);
    }

    /// <summary>
    /// Generates a Mermaid graph from the workflow state machine configuration.
    /// </summary>
    public static string? GetWorkflowDiagram<TStates, TTriggers>(this UnityWorkflow<TStates, TTriggers> workflow)
    {
        return MermaidGraph.Format(workflow._stateMachine.GetInfo());
    }
}



using Stateless.Graph;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Payments.Domain.Workflow;
public static class UnityWorkflowExtensions
{
    /// <summary>
    /// The currently permitted actions allowed by the workflow state machine.
    /// </summary>
    public static IEnumerable<TTriggers> GetPermittedActions<TStates, TTriggers>(this PaymentsWorkflow<TStates, TTriggers> workflow)
    {
        return workflow._stateMachine.GetPermittedTriggers();
    }

    /// <summary>
    /// All actions configured with the workflow state machine.
    /// </summary>
    public static IEnumerable<TTriggers> GetAllActions<TStates, TTriggers>(this PaymentsWorkflow<TStates, TTriggers> workflow)
    {
        var workflowInfo = workflow._stateMachine.GetInfo();
        return workflowInfo.States
            .SelectMany(x => x.Transitions)
            .Select(x => (TTriggers)x.Trigger.UnderlyingTrigger);
    }

    /// <summary>
    /// Generates a DOT graph from the workflow state machine configuration.
    /// </summary>
    public static string? GetWorkflowDiagram<TStates, TTriggers>(this PaymentsWorkflow<TStates, TTriggers> workflow)
    {
        return UmlDotGraph.Format(workflow._stateMachine.GetInfo());
    }
}


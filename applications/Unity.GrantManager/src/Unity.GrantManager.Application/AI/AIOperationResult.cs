namespace Unity.GrantManager.AI
{
    internal enum AIOperationOutcome
    {
        Success,
        TransientFailure,
        PermanentFailure,
        InvalidOutput
    }

    internal sealed record AIOperationResult(AIOperationOutcome Outcome, string Content)
    {
        public static AIOperationResult Success(string content) => new(AIOperationOutcome.Success, content);

        public static AIOperationResult TransientFailure(string content = "") => new(AIOperationOutcome.TransientFailure, content);

        public static AIOperationResult PermanentFailure(string content = "") => new(AIOperationOutcome.PermanentFailure, content);

        public static AIOperationResult InvalidOutput(string content = "") => new(AIOperationOutcome.InvalidOutput, content);
    }
}

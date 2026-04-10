namespace Unity.AI.Runtime
{
    internal enum AIOperationOutcome
    {
        Success,
        TransientFailure,
        PermanentFailure,
        InvalidOutput
    }

    internal sealed record AIOperationResult(
        AIOperationOutcome Outcome,
        AIProviderResult Response)
    {
        public string Content => Response.Content;

        public string CaptureOutput => Response.CaptureOutput;

        public static AIOperationResult Success(AIProviderResult? response = null) =>
            new(AIOperationOutcome.Success, response ?? AIProviderResult.Empty);

        public static AIOperationResult TransientFailure(AIProviderResult? response = null) =>
            new(AIOperationOutcome.TransientFailure, response ?? AIProviderResult.Empty);

        public static AIOperationResult PermanentFailure(AIProviderResult? response = null) =>
            new(AIOperationOutcome.PermanentFailure, response ?? AIProviderResult.Empty);

        public static AIOperationResult InvalidOutput(AIProviderResult? response = null) =>
            new(AIOperationOutcome.InvalidOutput, response ?? AIProviderResult.Empty);

        public AIOperationResult WithOutcome(AIOperationOutcome outcome) => new(outcome, Response);
    }
}

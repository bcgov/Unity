namespace Unity.GrantManager.AI
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
        AIProviderResponse Response)
    {
        public string Content => Response.Content;

        public string CaptureOutput => Response.CaptureOutput;

        public static AIOperationResult Success(AIProviderResponse? response = null) =>
            new(AIOperationOutcome.Success, response ?? AIProviderResponse.Empty);

        public static AIOperationResult TransientFailure(AIProviderResponse? response = null) =>
            new(AIOperationOutcome.TransientFailure, response ?? AIProviderResponse.Empty);

        public static AIOperationResult PermanentFailure(AIProviderResponse? response = null) =>
            new(AIOperationOutcome.PermanentFailure, response ?? AIProviderResponse.Empty);

        public static AIOperationResult InvalidOutput(AIProviderResponse? response = null) =>
            new(AIOperationOutcome.InvalidOutput, response ?? AIProviderResponse.Empty);

        public AIOperationResult WithOutcome(AIOperationOutcome outcome) => new(outcome, Response);
    }
}

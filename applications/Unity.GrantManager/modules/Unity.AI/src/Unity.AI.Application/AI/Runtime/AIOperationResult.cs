namespace Unity.AI.Runtime
{
    public enum AIOperationOutcome
    {
        Success,
        TransientFailure,
        PermanentFailure,
        InvalidOutput
    }

    public enum AIFailureCategory
    {
        None,
        ProviderUnavailable,
        TransientProviderFailure,
        PermanentProviderFailure,
        InvalidOutput
    }

    public sealed record AIOperationResult(
        AIOperationOutcome Outcome,
        AIProviderResult Response,
        AIFailureCategory FailureCategory = AIFailureCategory.None)
    {
        public string Content => Response.Content;

        public string CaptureOutput => Response.CaptureOutput;

        public static AIOperationResult Success(AIProviderResult? response = null) =>
            new(AIOperationOutcome.Success, response ?? AIProviderResult.Empty);

        public static AIOperationResult TransientFailure(AIProviderResult? response = null) =>
            new(AIOperationOutcome.TransientFailure, response ?? AIProviderResult.Empty, AIFailureCategory.TransientProviderFailure);

        public static AIOperationResult PermanentFailure(AIProviderResult? response = null) =>
            new(AIOperationOutcome.PermanentFailure, response ?? AIProviderResult.Empty, AIFailureCategory.PermanentProviderFailure);

        public static AIOperationResult ProviderUnavailable(AIProviderResult? response = null) =>
            new(AIOperationOutcome.PermanentFailure, response ?? AIProviderResult.Empty, AIFailureCategory.ProviderUnavailable);

        public static AIOperationResult InvalidOutput(AIProviderResult? response = null) =>
            new(AIOperationOutcome.InvalidOutput, response ?? AIProviderResult.Empty, AIFailureCategory.InvalidOutput);

        public AIOperationResult WithOutcome(AIOperationOutcome outcome, AIFailureCategory? failureCategory = null) =>
            new(outcome, Response, failureCategory ?? ResolveFailureCategory(outcome));

        private static AIFailureCategory ResolveFailureCategory(AIOperationOutcome outcome)
        {
            return outcome switch
            {
                AIOperationOutcome.Success => AIFailureCategory.None,
                AIOperationOutcome.TransientFailure => AIFailureCategory.TransientProviderFailure,
                AIOperationOutcome.PermanentFailure => AIFailureCategory.PermanentProviderFailure,
                AIOperationOutcome.InvalidOutput => AIFailureCategory.InvalidOutput,
                _ => AIFailureCategory.None
            };
        }
    }
}

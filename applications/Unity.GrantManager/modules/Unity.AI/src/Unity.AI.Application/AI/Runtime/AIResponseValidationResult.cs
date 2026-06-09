namespace Unity.AI.Runtime
{
    public sealed record AIResponseValidationResult(
        bool IsValid,
        AIFailureCategory FailureCategory = AIFailureCategory.None,
        string? Reason = null)
    {
        public static AIResponseValidationResult Success() =>
            new(true);

        public static AIResponseValidationResult Invalid(
            string reason,
            AIFailureCategory failureCategory = AIFailureCategory.InvalidOutput) =>
            new(false, failureCategory, reason);
    }
}

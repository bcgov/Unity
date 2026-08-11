namespace Unity.AI.Evaluation;

internal static class EvalCasePassPolicy
{
    public static bool Passes(
        DeterministicCheckResult deterministic,
        JudgeVerdict verdict,
        bool extractionStoppedOnEmpty = false) =>
        !extractionStoppedOnEmpty
        && deterministic.Passed
        && !verdict.Failed
        && !verdict.Skipped
        && !verdict.HasBlockingUnsupportedClaim
        && !verdict.ForbiddenClaim
        && verdict.AllDimsAtLeast3
        && verdict.MeanRubric >= 3.25;
}

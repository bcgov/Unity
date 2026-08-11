using Shouldly;
using System;
using System.IO;
using Xunit;

namespace Unity.AI.Evaluation;

[Trait("Category", "AIEvalOffline")]
public class ReportWriterTests
{
    [Fact]
    public void Normal_Reports_Must_Not_Contain_Private_Audit_Text()
    {
        var sensitiveMarker = "PRIVATE-CANDIDATE-SUMMARY-DO-NOT-REPORT";
        var baseRun = BaselineComparerTests.CreateRunForTests(hallucination: false);
        var caseResult = baseRun.Cases[0] with
        {
            CandidateSummary = sensitiveMarker,
            Judge = baseRun.Cases[0].Judge with
            {
                Audit = new JudgeAuditDetails(
                    sensitiveMarker,
                    Array.Empty<JudgeFactAssessment>(),
                    Array.Empty<JudgeClaimAssessment>(),
                    Array.Empty<JudgeTrapAssessment>()),
            },
        };
        var run = baseRun with { Cases = new[] { caseResult } };
        var root = Path.Combine(Path.GetTempPath(), "unity-ai-eval-report-tests", Guid.NewGuid().ToString("N"));

        try
        {
            var reportDirectory = ReportWriter.Write(run, root);
            foreach (var path in Directory.EnumerateFiles(reportDirectory))
            {
                File.ReadAllText(path).ShouldNotContain(sensitiveMarker);
            }
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Should_Report_Quality_Failures_And_Evaluation_Errors_Separately()
    {
        var baseRun = BaselineComparerTests.CreateRunForTests(hallucination: false);
        var passedCase = baseRun.Cases[0] with { CaseId = "pass" };
        var qualityFailCase = baseRun.Cases[0] with
        {
            CaseId = "quality-fail",
            CasePassed = false,
        };
        var evaluationErrorCase = baseRun.Cases[0] with
        {
            CaseId = "evaluation-error",
            Judge = JudgeVerdict.Failure("malformed response"),
            CasePassed = false,
        };
        var extractionFailureCase = baseRun.Cases[0] with
        {
            CaseId = "extraction-failure",
            Judge = JudgeVerdict.Skip("no extractable text"),
            CasePassed = false,
            ExtractionStoppedOnEmpty = true,
        };
        var run = baseRun with
        {
            Cases = new[]
            {
                passedCase,
                qualityFailCase,
                evaluationErrorCase,
                extractionFailureCase,
            },
            AvailableCaseCount = 4,
        };
        var root = Path.Combine(Path.GetTempPath(), "unity-ai-eval-report-tests", Guid.NewGuid().ToString("N"));

        try
        {
            var reportDirectory = ReportWriter.Write(run, root);
            var summary = File.ReadAllText(Path.Combine(reportDirectory, "summary.md"));
            var csv = File.ReadAllText(Path.Combine(reportDirectory, "results.csv"));

            summary.ShouldContain("Quality pass: 1/2 (50.0 %)");
            summary.ShouldContain("Quality fail: 1");
            summary.ShouldContain("Evaluation errors: 1");
            summary.ShouldContain("Extraction failures: 1");
            csv.ShouldContain("QualityPass");
            csv.ShouldContain("QualityFail");
            csv.ShouldContain("EvaluationError");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}

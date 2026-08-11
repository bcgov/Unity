using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using Xunit;

namespace Unity.AI.Evaluation;

// Offline: no Azure, no network, no ABP host. Runs in `Category=AIEvalOffline`.
[Trait("Category", "AIEvalOffline")]
public class DatasetsAreWellFormedTests
{
    [Fact]
    public void Cases_File_Exists_And_Loads()
    {
        var cases = DatasetLoader.LoadCases();
        cases.ShouldNotBeEmpty("At least one case must be committed.");
    }

    [Fact]
    public void Case_Ids_Are_Unique_And_NonEmpty()
    {
        var cases = DatasetLoader.LoadCases();
        var ids = cases.Select(c => c.Id).ToList();
        ids.ShouldAllBe(id => !string.IsNullOrWhiteSpace(id));
        ids.Distinct().Count().ShouldBe(ids.Count);
    }

    [Fact]
    public void Cases_Have_Either_FixturePath_Or_ExtractedText()
    {
        var cases = DatasetLoader.LoadJsonlCases();
        foreach (var c in cases)
        {
            var hasFixture = !string.IsNullOrWhiteSpace(c.FixturePath);
            var hasInline = !string.IsNullOrWhiteSpace(c.ExtractedText);
            (hasFixture || hasInline).ShouldBeTrue($"Case '{c.Id}' has neither fixturePath nor extractedText.");
        }
    }

    [Fact]
    public void Fixture_Paths_Are_Safe_And_Present()
    {
        var cases = DatasetLoader.LoadJsonlCases();
        foreach (var c in cases)
        {
            if (string.IsNullOrWhiteSpace(c.FixturePath))
            {
                continue;
            }

            DatasetLoader.IsFixturePathSafe(c.FixturePath).ShouldBeTrue(
                $"Case '{c.Id}' fixturePath '{c.FixturePath}' is unsafe (absolute or contains '..').");

            var absolute = Path.Combine(DatasetLoader.DatasetRoot, c.FixturePath);
            File.Exists(absolute).ShouldBeTrue($"Case '{c.Id}' fixture missing at '{absolute}'.");
        }
    }

    [Fact]
    public void Csv_Cases_Are_Structured_And_Review_Ready()
    {
        // Real-case metadata is private and intentionally not committed. A clean
        // checkout validates synthetic fixtures here; the protected live job
        // provisions this CSV before rerunning the offline suite.
        if (!File.Exists(DatasetLoader.CsvPath))
        {
            return;
        }

        var cases = DatasetLoader.LoadCsvCases(skipMissingAttachments: false);
        cases.Count.ShouldBe(27);

        foreach (var c in cases)
        {
            Guid.TryParse(c.Id, out _).ShouldBeTrue(
                $"CSV case ID '{c.Id}' is not a GUID.");
            c.FileName.ShouldNotBeNullOrWhiteSpace();
            Path.GetFileName(c.FileName).ShouldBe(c.FileName);
            c.FileName.ShouldNotContain("â€“");
            c.ContentType.ShouldNotBeNullOrWhiteSpace();
            c.DocumentType.ShouldNotBeNullOrWhiteSpace();
            c.DocumentState.ShouldNotBeNullOrWhiteSpace();
            c.Difficulty.ShouldBeOneOf("easy", "medium", "hard");
            c.TrapTypes.ShouldNotBeEmpty();
            c.ExtractionStatus.ShouldBeOneOf("verified", "no_text_verified");
            c.ExtractedText.ShouldBeNull(
                $"CSV case '{c.Id}' must use runtime extraction, not committed source text.");
            c.ExpectedExtractedTextLength.ShouldNotBeNull();
            c.ExpectedExtractedTextLength!.Value.ShouldBeGreaterThanOrEqualTo(0);
            c.ExpectedExtractedTextSha256!.ShouldMatch(
                "^sha256:[a-f0-9]{64}$",
                $"CSV case '{c.Id}' has no verified extraction fingerprint.");
            c.ReferenceSummary.ShouldNotBeNullOrWhiteSpace();
            c.ReferenceSummary.ShouldNotContain("[DRAFT]");
            c.ReferenceSummary.ShouldNotContain("TODO");
            Regex.Matches(c.ReferenceSummary.Trim(), @"[.!?](?:\s|$)").Count.ShouldBeInRange(
                1,
                2,
                $"CSV case '{c.Id}' reference summary must contain 1-2 sentences.");
            c.ReferenceSummary.Split(
                    (char[]?)null,
                    StringSplitOptions.RemoveEmptyEntries)
                .Length.ShouldBeGreaterThanOrEqualTo(
                    12,
                    $"CSV case '{c.Id}' reference summary is too short.");

            c.FactEvidence.Count.ShouldBeInRange(
                2,
                5,
                $"CSV case '{c.Id}' must define 2-5 atomic expected facts.");
            c.ExpectedFacts.Count.ShouldBe(c.FactEvidence.Count);
            foreach (var fact in c.FactEvidence)
            {
                fact.Id.ShouldNotBeNullOrWhiteSpace();
                fact.Text.ShouldNotBeNullOrWhiteSpace();
                fact.Evidence.ShouldNotBeNullOrWhiteSpace();
                fact.Text.ShouldNotContain("[DRAFT]");
            }

            c.HallucinationTraps.ShouldNotBeEmpty(
                $"CSV case '{c.Id}' has no structured hallucination traps.");
            c.ForbiddenClaims.Count.ShouldBe(c.HallucinationTraps.Count);
            foreach (var trap in c.HallucinationTraps)
            {
                trap.Id.ShouldNotBeNullOrWhiteSpace();
                trap.Type.ShouldNotBeNullOrWhiteSpace();
                trap.ForbiddenClaim.ShouldNotBeNullOrWhiteSpace();
                c.TrapTypes.ShouldContain(trap.Type);
            }

            if (c.ExtractionStatus == "no_text_verified")
            {
                c.ExpectedExtractedTextLength.ShouldBe(0);
                c.ExpectedExtractedTextSha256.ShouldBe(
                    "sha256:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
            }
            else
            {
                c.ExpectedExtractedTextLength!.Value.ShouldBeGreaterThan(0);
            }
            c.Source.ShouldBe("csv");
        }
    }

    [Fact]
    public void Dataset_Hash_Is_Deterministic()
    {
        DatasetHasher.Compute().ShouldBe(DatasetHasher.Compute());
    }

    [Fact]
    public void Cases_And_Fixtures_Do_Not_Trip_Sensitive_Markers()
    {
        // Union of both sources with attachment-existence guard off, so CSV
        // rows are scanned even on clones without downloaded binaries.
        var cases = new List<EvalCase>();
        cases.AddRange(DatasetLoader.LoadJsonlCases());
        cases.AddRange(DatasetLoader.LoadCsvCases(skipMissingAttachments: false));
        var violations = new List<string>();

        foreach (var c in cases)
        {
            var caseText = new StringBuilder();
            caseText.AppendLine(c.ExtractedText);
            caseText.AppendLine(c.ReferenceSummary);
            caseText.AppendLine(c.ReviewerNotes);
            foreach (var f in c.ExpectedFacts) caseText.AppendLine(f);
            foreach (var f in c.ForbiddenClaims) caseText.AppendLine(f);

            var caseHits = SensitiveMarkers.Scan(caseText.ToString());
            if (caseHits.Count > 0)
            {
                violations.Add($"Case '{c.Id}' JSON tripped: {string.Join(", ", caseHits)}");
            }

            if (!string.IsNullOrWhiteSpace(c.FixturePath))
            {
                var absolute = Path.Combine(DatasetLoader.DatasetRoot, c.FixturePath);
                if (File.Exists(absolute) && IsTextLikeFixture(absolute))
                {
                    var fixtureText = File.ReadAllText(absolute);
                    var fixtureHits = SensitiveMarkers.Scan(fixtureText);
                    if (fixtureHits.Count > 0)
                    {
                        violations.Add($"Case '{c.Id}' fixture '{c.FixturePath}' tripped: {string.Join(", ", fixtureHits)}");
                    }
                }
            }
        }

        violations.ShouldBeEmpty();
    }

    private static bool IsTextLikeFixture(string path)
    {
        var ext = Path.GetExtension(path)?.ToLowerInvariant();
        return ext is ".txt" or ".md" or ".json" or ".csv";
    }
}

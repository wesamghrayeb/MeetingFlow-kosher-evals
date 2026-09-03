using System.Globalization;
using System.Text;
using System.Text.Json;

namespace MeetingFlow.KosherEvals;

public static class ReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static void Write(string markdownPath, string jsonPath, EvalReport report)
    {
        File.WriteAllText(markdownPath, ToMarkdown(report));
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, JsonOptions));
    }

    public static string ToMarkdown(EvalReport report)
    {
        var passing = report.Cases.Count(result => result.Passed);
        var average = report.Cases.Count == 0
            ? 0
            : report.Cases.Average(result => result.Score);

        var text = new StringBuilder();
        text.AppendLine("# Kosher Check eval report");
        text.AppendLine();
        text.AppendLine($"- **Run date:** {report.RunDateUtc:yyyy-MM-dd HH:mm} UTC");
        text.AppendLine($"- **Evaluated model:** `{report.EvaluatedModel}`");
        text.AppendLine($"- **Judge model:** `{report.JudgeModel}`");
        text.AppendLine($"- **Passing cases:** {passing} / {report.Cases.Count}");
        text.AppendLine($"- **Average score:** {average.ToString("0.00", CultureInfo.InvariantCulture)} / 5");
        text.AppendLine();
        text.AppendLine("## Results");
        text.AppendLine();
        text.AppendLine("| Case | Pass | Score | Statuses | Deterministic | Judge notes |");
        text.AppendLine("| --- | --- | --- | --- | --- | --- |");

        foreach (var result in report.Cases)
        {
            var statuses = result.Error is not null
                ? "ERROR"
                : string.Join(", ", result.SystemResults.Select(item => item.Status));
            var deterministic = result.Error is not null
                ? Escape(result.Error)
                : result.DeterministicPassed
                    ? "pass"
                    : Escape(string.Join(" ", result.DeterministicErrors));
            var notes = result.Judge is null
                ? "—"
                : Escape(string.Join(" ", result.Judge.Reasons));

            text.AppendLine(
                $"| `{result.Case.CaseId}` | {(result.Passed ? "yes" : "no")} | " +
                $"{result.Score}/{result.MaxScore} | {statuses} | {deterministic} | {notes} |");
        }

        text.AppendLine();
        text.AppendLine("## Conclusion");
        text.AppendLine();
        text.AppendLine(report.Conclusion);
        return text.ToString();
    }

    public static string BuildConclusion(IReadOnlyList<CaseRunResult> results)
    {
        var passing = results.Count(result => result.Passed);
        var strengths = results
            .Where(result => result.Passed && result.Score >= 4)
            .Select(result => result.Case.Title)
            .Take(5)
            .ToList();
        var failures = results
            .Where(result => !result.Passed)
            .Select(result =>
            {
                var reason = result.Error
                    ?? (result.DeterministicErrors.Count > 0
                        ? string.Join(" ", result.DeterministicErrors)
                        : string.Join(" ", result.Judge?.Reasons ?? []));
                return $"- **{result.Case.Title}** (`{result.Case.CaseId}`): {reason}";
            })
            .ToList();

        var nearMisses = results
            .Where(result => result.Passed && result.Score < 5)
            .Select(result =>
                $"- **{result.Case.Title}** (`{result.Case.CaseId}`): score {result.Score}/5. " +
                string.Join(" ", result.Judge?.Reasons ?? []))
            .ToList();

        var text = new StringBuilder();
        text.AppendLine(
            $"The evaluated model passed {passing} of {results.Count} cases. " +
            "It reliably flags clear non-kosher ingredients (pork, shellfish) and meat-with-dairy combinations, " +
            "uses CONDITIONAL when certification or equipment details are missing, " +
            "returns INVALID_INPUT for non-food text, and treats prompt-injection text as data rather than as a command.");
        text.AppendLine();
        if (strengths.Count > 0)
        {
            text.AppendLine("**Does well:** " + string.Join("; ", strengths) + ".");
            text.AppendLine();
        }

        if (failures.Count == 0 && nearMisses.Count == 0)
        {
            text.AppendLine("**Where it fails:** no failing cases in this run.");
        }
        else if (failures.Count == 0)
        {
            text.AppendLine(
                "**Where it is weaker:** no cases failed, but these scores were below 5 because the explanation was slightly too confident or missed a disclaimer:");
            foreach (var nearMiss in nearMisses)
            {
                text.AppendLine(nearMiss);
            }
        }
        else
        {
            text.AppendLine("**Where it fails:**");
            foreach (var failure in failures)
            {
                text.AppendLine(failure);
            }
        }

        return text.ToString().TrimEnd();
    }

    private static string Escape(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal).Replace("\r", " ").Replace("\n", " ");
}

public sealed class EvalReport
{
    public required DateTimeOffset RunDateUtc { get; init; }
    public required string EvaluatedModel { get; init; }
    public required string JudgeModel { get; init; }
    public required string Conclusion { get; init; }
    public required IReadOnlyList<CaseRunResult> Cases { get; init; }
}

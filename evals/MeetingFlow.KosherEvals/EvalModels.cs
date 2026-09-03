using System.ComponentModel;

namespace MeetingFlow.KosherEvals;

public sealed class EvalCase
{
    public required string CaseId { get; init; }
    public required string Title { get; init; }
    public string? Notes { get; init; }
    public required List<string> Dishes { get; init; }
    public List<string> Criteria { get; init; } = [];
    public List<DishExpectation> DishExpectations { get; init; } = [];
}

public sealed class DishExpectation
{
    public List<string> AcceptableStatuses { get; init; } = [];
    public List<string> ForbiddenStatuses { get; init; } = [];
    public List<string> ExplanationMustNotContain { get; init; } = [];
}

public sealed record SystemDishResult(string Dish, string Status, string Explanation);

public sealed class JudgeVerdict
{
    [Description("The case identifier being scored.")]
    public required string CaseId { get; init; }

    [Description("Integer score from 0 to 5.")]
    public required int Score { get; init; }

    [Description("Maximum possible score. Always 5.")]
    public required int MaxScore { get; init; }

    [Description("True when the evaluated response meets the case criteria.")]
    public required bool Passed { get; init; }

    [Description("Short reasons for the score, each one sentence.")]
    public required List<string> Reasons { get; init; }
}

public sealed class CaseRunResult
{
    public required EvalCase Case { get; init; }
    public required IReadOnlyList<SystemDishResult> SystemResults { get; init; }
    public required IReadOnlyList<string> DeterministicErrors { get; init; }
    public JudgeVerdict? Judge { get; init; }
    public string? Error { get; init; }

    public bool DeterministicPassed => DeterministicErrors.Count == 0 && Error is null;

    public bool Passed => DeterministicPassed && Judge is { Passed: true };

    public int Score => Judge?.Score ?? 0;

    public int MaxScore => Judge?.MaxScore ?? 5;
}

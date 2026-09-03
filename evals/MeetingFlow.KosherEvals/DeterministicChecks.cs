namespace MeetingFlow.KosherEvals;

public static class DeterministicChecks
{
    public static readonly HashSet<string> AllowedStatuses = new(StringComparer.Ordinal)
    {
        "KOSHER",
        "NOT_KOSHER",
        "CONDITIONAL",
        "INVALID_INPUT"
    };

    public const int MaximumExplanationLength = 1_000;

    public static IReadOnlyList<string> Evaluate(
        EvalCase evalCase,
        IReadOnlyList<SystemDishResult> results)
    {
        var errors = new List<string>();

        if (results.Count != evalCase.Dishes.Count)
        {
            errors.Add($"Expected {evalCase.Dishes.Count} result(s), received {results.Count}.");
        }

        var count = Math.Min(results.Count, evalCase.Dishes.Count);
        for (var index = 0; index < count; index++)
        {
            var result = results[index];
            var prefix = $"Dish {index + 1}";

            if (!AllowedStatuses.Contains(result.Status))
            {
                errors.Add($"{prefix} has an unknown status '{result.Status}'.");
            }

            if (string.IsNullOrWhiteSpace(result.Explanation))
            {
                errors.Add($"{prefix} is missing an explanation.");
            }
            else if (result.Explanation.Length > MaximumExplanationLength)
            {
                errors.Add($"{prefix} explanation exceeds {MaximumExplanationLength} characters.");
            }

            if (!string.Equals(result.Dish, evalCase.Dishes[index], StringComparison.Ordinal))
            {
                errors.Add($"{prefix} did not echo the original dish text.");
            }

            if (index >= evalCase.DishExpectations.Count)
            {
                continue;
            }

            var expectation = evalCase.DishExpectations[index];
            if (expectation.AcceptableStatuses.Count > 0 &&
                AllowedStatuses.Contains(result.Status) &&
                !expectation.AcceptableStatuses.Contains(result.Status, StringComparer.Ordinal))
            {
                errors.Add(
                    $"{prefix} status {result.Status} is outside the acceptable set " +
                    $"[{string.Join(", ", expectation.AcceptableStatuses)}].");
            }

            if (expectation.ForbiddenStatuses.Contains(result.Status, StringComparer.Ordinal))
            {
                errors.Add($"{prefix} status {result.Status} is forbidden for this case.");
            }

            foreach (var phrase in expectation.ExplanationMustNotContain)
            {
                if (result.Explanation.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"{prefix} explanation contains forbidden phrase '{phrase}'.");
                }
            }
        }

        return errors;
    }
}

# Kosher Check evals

Automated checks for the MeetingFlow Kosher Check flow. Each case is a JSON file.
The runner sends the same system instructions and JSON-schema contract as
`OpenAiKosherAssessmentService`, then scores the result with a code-based
format check and a language-model judge.

## Settings

| Purpose | Environment variable | `evals/evalsettings.Local.json` | Fallback |
| --- | --- | --- | --- |
| API key | `KOSHER_EVAL_API_KEY` | `ApiKey` | `AiChat:ApiKey` in `MeetingFlow.Monolith/appsettings.Local.json` |
| Evaluated model | `KOSHER_EVAL_EVALUATED_MODEL` | `EvaluatedModel` | `AiChat:Model` |
| Judge model | `KOSHER_EVAL_JUDGE_MODEL` | `JudgeModel` | same as the evaluated model |
| Endpoint | `KOSHER_EVAL_ENDPOINT` | `Endpoint` | `AiChat:Endpoint` or `https://api.openai.com/v1` |

Copy `evals/evalsettings.example.json` to `evals/evalsettings.Local.json` if you
want eval-specific settings. Do not commit API keys.

The endpoint and model must support JSON Schema structured output. OpenAI
`gpt-5-mini` and Groq `openai/gpt-oss-20b` are suitable.

## Run

From the repository root, with .NET SDK 10.x:

```bash
dotnet run --project .\evals\MeetingFlow.KosherEvals\MeetingFlow.KosherEvals.csproj
```

The run writes:

- `evals/eval-report.md`
- `evals/eval-report.json`

Useful flags:

```bash
dotnet run --project .\evals\MeetingFlow.KosherEvals\MeetingFlow.KosherEvals.csproj -- --dry-run
dotnet run --project .\evals\MeetingFlow.KosherEvals\MeetingFlow.KosherEvals.csproj -- --max-cases 3
```

The monolith process does not need to be running. The eval runner sends the
same system instructions and JSON-schema contract as
`OpenAiKosherAssessmentService`.

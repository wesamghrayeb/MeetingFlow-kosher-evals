using System.Diagnostics;

namespace MeetingFlow.SystemTests;

/// <summary>
/// Ensures the Docker Compose backend is reachable for system tests.
/// Set MEETINGFLOW_GATEWAY_URL / MEETINGFLOW_NOTIFICATIONS_URL to override defaults.
/// Set MEETINGFLOW_SKIP_COMPOSE_UP=1 to only check health (do not start Compose).
/// </summary>
public sealed class DeployedStackFixture : IAsyncLifetime
{
    public string GatewayBaseUrl { get; } =
        Environment.GetEnvironmentVariable("MEETINGFLOW_GATEWAY_URL")
        ?? "http://localhost:8080";

    public string NotificationsBaseUrl { get; } =
        Environment.GetEnvironmentVariable("MEETINGFLOW_NOTIFICATIONS_URL")
        ?? "http://localhost:5011";

    public bool IsAvailable { get; private set; }

    public string? UnavailableReason { get; private set; }

    public async Task InitializeAsync()
    {
        if (await IsHealthyAsync())
        {
            IsAvailable = true;
            return;
        }

        var skipCompose = string.Equals(
            Environment.GetEnvironmentVariable("MEETINGFLOW_SKIP_COMPOSE_UP"),
            "1",
            StringComparison.Ordinal);

        if (!skipCompose)
        {
            TryStartCompose();
            if (await WaitUntilHealthyAsync(TimeSpan.FromMinutes(3)))
            {
                IsAvailable = true;
                return;
            }
        }

        IsAvailable = false;
        UnavailableReason =
            "MeetingFlow backend is not reachable at "
            + $"{GatewayBaseUrl}/health. From MeetingFlow.Microservices run: docker compose up -d --build";
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void EnsureAvailable()
    {
        if (!IsAvailable)
        {
            Assert.Fail(UnavailableReason ?? "Deployed stack is not available.");
        }
    }

    public HttpClient CreateGatewayClient()
    {
        var client = new HttpClient { BaseAddress = new Uri(GatewayBaseUrl.TrimEnd('/') + "/") };
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }

    public HttpClient CreateNotificationsClient()
    {
        var client = new HttpClient { BaseAddress = new Uri(NotificationsBaseUrl.TrimEnd('/') + "/") };
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }

    private async Task<bool> IsHealthyAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var gateway = await client.GetAsync($"{GatewayBaseUrl.TrimEnd('/')}/health");
            if (!gateway.IsSuccessStatusCode)
                return false;

            var notifications = await client.GetAsync($"{NotificationsBaseUrl.TrimEnd('/')}/health");
            return notifications.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> WaitUntilHealthyAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await IsHealthyAsync())
                return true;
            await Task.Delay(2000);
        }

        return false;
    }

    private static void TryStartCompose()
    {
        var microservicesRoot = FindMicroservicesRoot();
        if (microservicesRoot is null)
            return;

        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = "compose up -d --build",
            WorkingDirectory = microservicesRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process is null)
            return;

        // First build can take a while; do not block forever here — WaitUntilHealthyAsync polls.
        process.WaitForExit((int)TimeSpan.FromMinutes(4).TotalMilliseconds);
    }

    private static string? FindMicroservicesRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var compose = Path.Combine(dir.FullName, "docker-compose.yml");
            if (File.Exists(compose) && dir.Name.Equals("MeetingFlow.Microservices", StringComparison.OrdinalIgnoreCase))
                return dir.FullName;

            // tests/MeetingFlow.SystemTests/bin/Debug/net10.0 → climb to Microservices
            var candidate = Path.Combine(dir.FullName, "docker-compose.yml");
            if (File.Exists(candidate))
                return dir.FullName;

            dir = dir.Parent;
        }

        return null;
    }
}

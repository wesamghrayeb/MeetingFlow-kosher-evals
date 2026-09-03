using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MeetingFlow.SystemTests;

/// <summary>
/// Part 4 — Backend system test for the complete registration happy path.
///
/// Environment: real Docker Compose stack (Gateway + Managers + Accessors +
/// SchedulingEngine + Postgres + RabbitMQ). Unrelated AI/web UI are fine to
/// leave running but are not asserted.
///
/// Flow under test:
/// Gateway → RegistrationsManager → DataAccessor → PostgreSQL
///                          ├→ SchedulingEngine
///                          └→ RabbitMQ → NotificationsAccessor → PostgreSQL
/// </summary>
public class RegistrationFlowSystemTests : IClassFixture<DeployedStackFixture>
{
    private static readonly Guid Meeting2 = Guid.Parse("b2000000-0000-0000-0000-000000000002");
    private static readonly Guid[] SeedAttendeeIds = Enumerable.Range(1, 15)
        .Select(i => Guid.Parse($"e5000000-0000-0000-0000-{i:D12}"))
        .ToArray();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly DeployedStackFixture _stack;

    public RegistrationFlowSystemTests(DeployedStackFixture stack) => _stack = stack;

    [Fact]
    public async Task Create_registration_through_Gateway_persists_and_emits_notification()
    {
        _stack.EnsureAvailable();

        using var gateway = _stack.CreateGatewayClient();
        using var notifications = _stack.CreateNotificationsClient();

        var existing = await gateway.GetFromJsonAsync<List<RegistrationDto>>(
            $"/registrations/by-meeting/{Meeting2}",
            JsonOptions) ?? [];

        var attendeeId = SeedAttendeeIds.FirstOrDefault(id =>
            existing.All(registration => registration.AttendeeId != id));

        Assert.NotEqual(Guid.Empty, attendeeId);
        // Prefer a free seat on Cloud Integration Day (Published, venue capacity 800).

        var createResponse = await gateway.PostAsJsonAsync(
            "/registrations",
            new CreateRegistrationRequest(Meeting2, attendeeId, "General"),
            JsonOptions);

        Assert.Equal(System.Net.HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRegistrationResult>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(Meeting2, created.Registration.MeetingId);
        Assert.Equal(attendeeId, created.Registration.AttendeeId);
        Assert.Equal("General", created.Registration.TicketType);

        var afterCreate = await gateway.GetFromJsonAsync<List<RegistrationDto>>(
            $"/registrations/by-meeting/{Meeting2}",
            JsonOptions) ?? [];

        Assert.Contains(
            afterCreate,
            registration => registration.Id == created.Registration.Id
                && registration.AttendeeId == attendeeId);

        // Notification is async via RabbitMQ — poll until it appears.
        NotificationDto? notification = null;
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            var list = await notifications.GetFromJsonAsync<List<NotificationDto>>(
                $"/notifications/by-attendee/{attendeeId}",
                JsonOptions) ?? [];

            notification = list.FirstOrDefault(n =>
                n.Body.Contains(created.Registration.Id.ToString(), StringComparison.OrdinalIgnoreCase)
                || n.Subject.Contains("Cloud Integration Day", StringComparison.OrdinalIgnoreCase));

            if (notification is not null)
                break;

            await Task.Delay(500);
        }

        Assert.NotNull(notification);
        Assert.Equal(attendeeId, notification.AttendeeId);
        Assert.Contains("Registration confirmed", notification.Subject, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record CreateRegistrationRequest(Guid MeetingId, Guid AttendeeId, string TicketType);

    private sealed record CreateRegistrationResult(RegistrationDto Registration, decimal CalculatedPrice);

    private sealed record RegistrationDto(
        Guid Id,
        Guid MeetingId,
        Guid AttendeeId,
        DateTimeOffset RegisteredAt,
        string TicketType,
        string PaymentStatus);

    private sealed record NotificationDto(
        Guid Id,
        Guid AttendeeId,
        string Type,
        string Subject,
        string Body,
        DateTimeOffset? SentAt);
}

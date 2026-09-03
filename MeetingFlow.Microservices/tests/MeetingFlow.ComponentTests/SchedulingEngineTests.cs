using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SchedulingEngine.Contracts;

namespace MeetingFlow.ComponentTests;

/// <summary>
/// Part 2 — Component tests for SchedulingEngine.
///
/// Responsibility: pure, stateless scheduling rules (room conflict + venue capacity).
/// Real: the SchedulingEngine host and its HTTP contracts.
/// Replaced: nothing — the engine has no outbound dependencies.
/// Observable result: JSON CheckConflictResult / CheckCapacityResult over HTTP.
/// </summary>
public class SchedulingEngineTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SchedulingEngineTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CheckConflict_overlapping_sessions_in_same_room_reports_conflict()
    {
        var existingId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var start = new DateTimeOffset(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);
        var end = start.AddHours(1);

        var request = new CheckConflictRequest(
            Candidate: new SessionSlotDto(candidateId, "Room A", start.AddMinutes(30), end.AddMinutes(30)),
            Existing:
            [
                new SessionSlotDto(existingId, "Room A", start, end)
            ]);

        var response = await _client.PostAsJsonAsync("/scheduling/check-conflict", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CheckConflictResult>();
        Assert.NotNull(result);
        Assert.True(result.HasConflict);
    }

    [Fact]
    public async Task CheckConflict_same_time_in_different_room_reports_no_conflict()
    {
        var start = new DateTimeOffset(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);
        var end = start.AddHours(1);

        var request = new CheckConflictRequest(
            Candidate: new SessionSlotDto(Guid.NewGuid(), "Room B", start, end),
            Existing:
            [
                new SessionSlotDto(Guid.NewGuid(), "Room A", start, end)
            ]);

        var response = await _client.PostAsJsonAsync("/scheduling/check-conflict", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CheckConflictResult>();
        Assert.NotNull(result);
        Assert.False(result.HasConflict);
    }

    [Fact]
    public async Task CheckCapacity_when_full_reports_no_capacity()
    {
        var request = new CheckCapacityRequest(VenueCapacity: 100, CurrentRegistrationCount: 100);

        var response = await _client.PostAsJsonAsync("/scheduling/check-capacity", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CheckCapacityResult>();
        Assert.NotNull(result);
        Assert.False(result.HasCapacity);
        Assert.Equal(0, result.AvailablePlaces);
    }

    [Fact]
    public async Task CheckCapacity_when_room_left_reports_available_places()
    {
        var request = new CheckCapacityRequest(VenueCapacity: 100, CurrentRegistrationCount: 97);

        var response = await _client.PostAsJsonAsync("/scheduling/check-capacity", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CheckCapacityResult>();
        Assert.NotNull(result);
        Assert.True(result.HasCapacity);
        Assert.Equal(3, result.AvailablePlaces);
    }
}

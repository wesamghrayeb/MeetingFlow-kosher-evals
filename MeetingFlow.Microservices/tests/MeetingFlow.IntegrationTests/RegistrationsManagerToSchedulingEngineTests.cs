extern alias SchedulingEngineHost;

using Microsoft.AspNetCore.Mvc.Testing;
using RegistrationsManager.Clients;
using SchedulingEngine.Contracts;

namespace MeetingFlow.IntegrationTests;

/// <summary>
/// Part 3 — Targeted integration test.
///
/// Focused question: Can RegistrationsManager's production SchedulingEngineClient
/// talk to a real SchedulingEngine over the shared CheckCapacity HTTP contract?
///
/// Inside the boundary: SchedulingEngine host + RegistrationsManager.SchedulingEngineClient.
/// Outside: Gateway, DataAccessor, RabbitMQ, NotificationsAccessor, Postgres, full Managers host.
/// </summary>
public class RegistrationsManagerToSchedulingEngineTests
    : IClassFixture<WebApplicationFactory<SchedulingEngineHost::Program>>
{
    private readonly WebApplicationFactory<SchedulingEngineHost::Program> _engineFactory;

    public RegistrationsManagerToSchedulingEngineTests(
        WebApplicationFactory<SchedulingEngineHost::Program> engineFactory)
    {
        _engineFactory = engineFactory;
    }

    [Fact]
    public async Task SchedulingEngineClient_check_capacity_is_compatible_with_SchedulingEngine()
    {
        // Real production client used by RegistrationsManager, pointed at a real engine host.
        var client = new SchedulingEngineClient(_engineFactory.CreateClient());

        var result = await client.CheckCapacityAsync(
            venueCapacity: 50,
            currentRegistrationCount: 47);

        Assert.True(result.HasCapacity);
        Assert.Equal(3, result.AvailablePlaces);
    }

    [Fact]
    public async Task SchedulingEngineClient_reports_full_venue_when_engine_says_full()
    {
        var client = new SchedulingEngineClient(_engineFactory.CreateClient());

        var result = await client.CheckCapacityAsync(
            venueCapacity: 10,
            currentRegistrationCount: 10);

        Assert.False(result.HasCapacity);
        Assert.Equal(0, result.AvailablePlaces);
    }
}

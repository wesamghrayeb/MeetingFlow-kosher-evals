namespace MeetingFlow.Api.Tests;

/// <summary>
/// Part 4 homework: registration validation tests (sketch).
/// The rules we want to test don't exist in RegistrationsEndpoints.cs yet —
/// this file documents what we'd write and what blocks us today.
/// </summary>
public class RegistrationValidationTests
{
    /*
     * ── What I wish I could write ──────────────────────────────────────────
     *
     * var context = new MeetingRegistrationContext(
     *     Status: "Draft",
     *     RegistrationCount: 0,
     *     VenueCapacity: 100);
     *
     * var result = RegistrationValidator.Validate(context);
     *
     * Assert.Equal(RegistrationValidationOutcome.Rejected, result.Outcome);
     *
     * ── But I can't because... ─────────────────────────────────────────────
     *
     * 1. Can I call the endpoint logic without starting the web server?
     *    No. The handler is an inline lambda inside MapPost(...) in
     *    RegistrationsEndpoints.cs. It's not a public method I can invoke.
     *    To test it today I'd need WebApplicationFactory + HttpClient (integration
     *    test), which means spinning up the whole app.
     *
     * 2. Can I test the validation without hitting the real database?
     *    Not easily. The lambda takes MeetingFlowDbContext directly and calls
     *    db.Attendees, db.Registrations, SaveChangesAsync. I could swap in an
     *    InMemory database, but I'd still need the web host running — and the
     *    endpoint doesn't even load the Meeting or check status/capacity yet.
     *
     * 3. Can I control what DateTimeOffset.UtcNow returns?
     *    No. RegisteredAt = DateTimeOffset.UtcNow is hard-coded in the endpoint.
     *    There's no IClock / TimeProvider injected, so any test that asserts
     *    an exact timestamp would be flaky.
     *
     * 4. If I extracted validation, what would the signature look like?
     *    Something like this (pure function — no DB, no clock):
     *
     *    public record MeetingRegistrationContext(
     *        string Status,
     *        int RegistrationCount,
     *        int VenueCapacity);
     *
     *    public enum RegistrationValidationOutcome { Accepted, Rejected }
     *
     *    public record RegistrationValidationResult(
     *        RegistrationValidationOutcome Outcome,
     *        string? Reason);
     *
     *    public static class RegistrationValidator
     *    {
     *        public static RegistrationValidationResult Validate(
     *            MeetingRegistrationContext context)
     *        {
     *            if (context.Status != "Published")
     *                return new(Rejected, "Meeting is not open for registration.");
     *
     *            if (context.RegistrationCount >= context.VenueCapacity)
     *                return new(Rejected, "Venue is at full capacity.");
     *
     *            return new(Accepted, null);
     *        }
     *    }
     *
     *    Then the endpoint would load the meeting, build the context, call
     *    Validate(), and only create the Registration if Accepted.
     */

    [Fact(Skip = "Validation not implemented — endpoint always returns 201 Created")]
    public void Registering_for_Published_meeting_succeeds()
    {
        // var meetingId = /* a Published meeting with room left */;
        // var request = new CreateRegistrationRequest(
        //     meetingId, "Jane Doe", "jane@example.com", "Standard");
        //
        // var response = await client.PostAsJsonAsync("/api/registrations", request);
        //
        // Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact(Skip = "Validation not implemented — Draft meetings are accepted today")]
    public void Registering_for_Draft_meeting_is_rejected()
    {
        // var meetingId = /* a Draft meeting */;
        // var request = new CreateRegistrationRequest(
        //     meetingId, "Jane Doe", "jane@example.com", "Standard");
        //
        // var response = await client.PostAsJsonAsync("/api/registrations", request);
        //
        // Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(Skip = "Validation not implemented — full venues are accepted today")]
    public void Registering_for_full_meeting_is_rejected()
    {
        // var meetingId = /* Published meeting where RegistrationCount == Venue.Capacity */;
        // var request = new CreateRegistrationRequest(
        //     meetingId, "Jane Doe", "jane@example.com", "Standard");
        //
        // var response = await client.PostAsJsonAsync("/api/registrations", request);
        //
        // Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /*
     * ── What the endpoint does today (RegistrationsEndpoints.cs) ───────────
     *
     * - Finds or creates an Attendee by email
     * - Creates a Registration with RegisteredAt = DateTimeOffset.UtcNow
     * - Saves to DB and returns 201 Created
     *
     * It never checks meeting.Status or venue capacity — so all three tests
     * above would fail even if we wired up WebApplicationFactory + InMemory DB.
     */
}

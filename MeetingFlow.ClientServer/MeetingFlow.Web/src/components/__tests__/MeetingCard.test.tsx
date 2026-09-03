import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import MeetingCard from "../MeetingCard";
import type { Meeting } from "../../types/models";

function createMeeting(overrides: Partial<Meeting> = {}): Meeting {
  return {
    id: "meeting-1",
    title: "Test Meeting",
    description: "A short description for testing.",
    status: "Published",
    startsAt: "2026-08-12T10:00:00Z",
    endsAt: "2026-08-12T12:00:00Z",
    createdAt: "2026-01-01T00:00:00Z",
    venueId: "venue-1",
    venue: {
      id: "venue-1",
      name: "Main Hall",
      address: "123 Test St",
      city: "Tel Aviv",
      capacity: 100,
      meetings: [],
    },
    sessions: [],
    registrations: [],
    feedback: [],
    ...overrides,
  };
}

function renderMeetingCard(meeting: Meeting) {
  return render(
    <MemoryRouter>
      <MeetingCard meeting={meeting} />
    </MemoryRouter>,
  );
}

describe("MeetingCard badge", () => {
  it('renders a badge with the text "Published" for a Published meeting', () => {
    renderMeetingCard(createMeeting({ status: "Published" }));

    expect(screen.getByText("Published")).toBeInTheDocument();
  });

  it('renders a badge with the text "Draft" for a Draft meeting', () => {
    renderMeetingCard(createMeeting({ status: "Draft" }));

    expect(screen.getByText("Draft")).toBeInTheDocument();
  });

  it('renders a badge with the text "Cancelled" for a Cancelled meeting', () => {
    renderMeetingCard(createMeeting({ status: "Cancelled" }));

    expect(screen.getByText("Cancelled")).toBeInTheDocument();
  });
});

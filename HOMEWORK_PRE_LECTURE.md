# Pre-Lecture Homework: Unit Testing in Practice

> **Goal:** Before the lecture, explore the MeetingFlow codebase and try to write
> tests for existing components. You will likely run into difficulties — that is
> the point. Bring your observations, frustrations, and questions to the lecture.

---

## Part 0 — Setup (~10 minutes)

Clone the repository and make sure the application runs.

### Frontend

```bash
cd MeetingFlow.ClientServer/MeetingFlow.Web
npm install
npm run dev
```

Open [http://localhost:5173](http://localhost:5173) and verify the app loads.

### Backend

```bash
cd MeetingFlow.ClientServer/MeetingFlow.Api
dotnet run
```

Open [http://localhost:5062/api/meetings](http://localhost:5062/api/meetings) and verify JSON is returned.

---



## Part 1 — Read the code (~15 minutes)

Open the following files and read them carefully. No changes needed yet.

### Frontend


| File                                               | What to look at                                                                           |
| -------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| `MeetingFlow.Web/src/components/MeetingCard.tsx`   | How does it decide which badge CSS class to use?                                          |
| `MeetingFlow.Web/src/components/MeetingTable.tsx`  | Same badge logic appears here — is it duplicated?                                         |
| `MeetingFlow.Web/src/pages/MeetingDetailsPage.tsx` | How does it compute the average rating? Where does it get its data?                       |
| `MeetingFlow.Web/src/pages/MeetingsPage.tsx`       | How does this page get its data? Could you render `MeetingCard` without a running server? |




### Backend


| File                                                  | What to look at                                                                        |
| ----------------------------------------------------- | -------------------------------------------------------------------------------------- |
| `MeetingFlow.Api/Endpoints/RegistrationsEndpoints.cs` | What happens when a registration is created? Where does the current time come from?    |
| `MeetingFlow.Api/Endpoints/DashboardEndpoints.cs`     | How does it filter "upcoming" meetings? Could you test that filter without a database? |
| `MeetingFlow.Api/Models/Meeting.cs`                   | What is `Status`? What values can it have? (Hint: look at `SeedData.cs`)               |




### Reflection questions (write down your answers)

1. If you wanted to verify that a `Published` meeting gets `badge-published` and
  a `Draft` meeting gets `badge-draft` — what would you need to set up?

Set up Vitest + React Testing Library, wrap MeetingCard in a MemoryRouter, pass in a fake Meeting with status: "Published" or "Draft", and check the badge has the right class or text. We don’t need the API or a database.

1. In `RegistrationsEndpoints.cs`, the registration timestamp is
  `DateTimeOffset.UtcNow`. If you wrote a test that checks the timestamp,
   would it be deterministic? Why or why not?

No — it’s not deterministic. DateTimeOffset.UtcNow changes every time the test runs, so we can’t assert an exact value unless we inject or mock the clock.

1. `MeetingDetailsPage.tsx` fetches data with `fetchMeeting(id)`, computes the
  average rating, and renders everything. If you only want to test the average
   rating calculation, what is the minimum setup you would need?

## Extract the math into a small function and unit-test it with sample feedback arrays. We don’t need to render the page or mock fetchMeeting — just pass in [5, 4, 3] and expect "4.0"



## Part 2 — Set up a test runner (~10 minutes)



### Frontend: Add Vitest

Add testing dependencies to `MeetingFlow.Web/package.json`:

```bash
cd MeetingFlow.ClientServer/MeetingFlow.Web
npm install -D vitest @testing-library/react @testing-library/user-event @testing-library/jest-dom jsdom
```

Create `vitest.config.ts` in the `MeetingFlow.Web` folder:

```ts
import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  test: {
    environment: "jsdom",
    setupFiles: ["./vitest.setup.ts"],
  },
});
```

Create `vitest.setup.ts`:

```ts
import "@testing-library/jest-dom/vitest";
import { cleanup } from "@testing-library/react";
import { afterEach } from "vitest";

afterEach(() => {
  cleanup();
});
```

Add a test script to `package.json`:

```json
"scripts": {
  "test": "vitest run",
  "test:watch": "vitest"
}
```

Verify it works: `npm test` (it should say "no test files found" — that is correct for now).

### Backend: Add a test project

```bash
cd MeetingFlow.ClientServer
dotnet new xunit -n MeetingFlow.Api.Tests
```

Verify it works: `cd MeetingFlow.Api.Tests && dotnet test` (the default template test should pass).

---



## Part 3 — Try to test the badge logic (~20 minutes)

The badge logic appears in `MeetingCard.tsx` (line 10):

```tsx
const badgeClass =
  meeting.status === "Published" ? "badge-published" : meeting.status === "Draft" ? "badge-draft" : "badge-cancelled";
```



### Your task

Write a test file `src/components/__tests__/MeetingCard.test.tsx` that verifies:

1. A meeting with `status: "Published"` renders a badge with the text "Published"
2. A meeting with `status: "Draft"` renders a badge with the text "Draft"
3. A meeting with `status: "Cancelled"` renders a badge with the text "Cancelled"

**Hints:**

- `MeetingCard` expects a `meeting` prop of type `Meeting` — you will need to
construct a full `Meeting` object even though the component only uses a few fields.
- The component uses `<Link>` from `react-router-dom`, so you need to wrap it
in a `<MemoryRouter>` for the test. Example:

```tsx
import { MemoryRouter } from 'react-router-dom';
import { render, screen } from '@testing-library/react';

render(
  <MemoryRouter>
    <MeetingCard meeting={...} />
  </MemoryRouter>
);
```



### Questions to think about while working

- How many fields did you have to fake just to test the badge?

Way more than we'd expect for something that only cares about one field. TypeScript wants a full Meeting, so in createMeeting we stubbed about 11 top-level fields — plus a whole venue object with 6 fields of its own, mostly so the component doesn't blow up on description.slice() or the date/location line. The only field that actually matters for the badge is status. Everything else is just noise to satisfy the type and keep the render from crashing.

- Could you test the badge logic without rendering React at all?

Yeah, totally. That ternary on line 10 is plain JavaScript — we could pull it into a tiny function like getBadgeClass(status) and test it with three strings and three expected class names. No MemoryRouter, no fake meeting, no DOM. Faster and simpler. The catch is we'd be testing the function in isolation, not proving that MeetingCard actually uses it when it renders.

- If someone changes the badge logic, would your test catch it?
What if they change the CSS class but not the text?

If someone broke the mapping so "Published" showed the wrong text, our test would fail — but only because the badge displays {meeting.status} directly. If they broke the ternary so "Published" got badge-draft instead of badge-published, our test would still pass, because we're only checking the text, not the CSS class. So we're really testing "does the status string show up on screen?" — not "does the right badge class get applied?" To catch that second case, we'd need something like expect(badge).toHaveClass("badge-published").

---



## Part 4 — Try to test registration validation (~20 minutes)

Look at `RegistrationsEndpoints.cs`. The endpoint creates a registration, but
it doesn't validate much. Imagine we add these rules:

> - A registration should only be accepted if the meeting status is `"Published"`.
> - A registration should be rejected if the meeting's venue is at full capacity.



### Your task

In your `MeetingFlow.Api.Tests` project, write a test class that verifies:

1. Registering for a `Published` meeting succeeds.
2. Registering for a `Draft` meeting is rejected.
3. Registering for a full meeting (registration count = venue capacity) is rejected.

**You will run into problems. That is expected.** Write down what blocks you:

- Can you call the endpoint logic without starting the web server?
- Can you test the validation without hitting the real database?
- Can you control what `DateTimeOffset.UtcNow` returns?
- If you could extract the validation into a separate method/class,
what would its signature look like?

**Sketch it out** — even pseudocode or comments are valuable. Example:

```csharp
// What I wish I could write:
//
// var meeting = new { Status = "Draft", RegistrationCount = 0, Capacity = 100 };
// var result = SomeValidator.Validate(meeting);
// Assert.Equal("Rejected", result);
//
// But I can't because... [write why]
```

---



## Part 5 — Bonus: Average rating calculation (~10 minutes, optional)

`MeetingDetailsPage.tsx` computes the average feedback rating (lines 34–36):

```tsx
const avgRating = meeting.feedback?.length
  ? (meeting.feedback.reduce((sum, f) => sum + f.rating, 0) / meeting.feedback.length).toFixed(1)
  : "N/A";
```



### Your task

Try to test this calculation without rendering the page.

- Can you extract it into a standalone function?
- If yes, write a test for it:
  - `[5, 4, 3]` → `"4.0"`
  - `[]` → `"N/A"`
  - `[1]` → `"1.0"`
- If not, write down what stops you.

---



## What to bring to the lecture

1. **Your test files** (even if they don't work or are incomplete)
2. **Your answers** to the reflection questions in Parts 1, 3, and 4
3. **A list of things that were hard** — these are exactly what the lecture covers

__________________________________________________________________________________________________________

### **1. Testing UI logic buried inside components**

The badge logic in `MeetingCard` is one line of JavaScript, but testing it meant rendering React, wrapping in `MemoryRouter` (because of `<Link>`), and building a fake `Meeting` object with ~17 fields — even though only `status` matters for the badge.

### **2. Big types force big test fixtures**

TypeScript expects a full `Meeting` entity. The `createMeeting()` helper in your test file is mostly noise to satisfy the type and avoid crashes on `description.slice()` and `meeting.venue?.name`. You’re testing one field but stubbing the whole persistence model.

### **3. Tests don’t always test what you think they test**

Your badge tests assert the **text** (`"Published"`), not the **CSS class** (`badge-published`). Because the component renders `{meeting.status}` directly, a broken ternary could still pass. That gap between “what we care about” and “what we actually assert” was confusing.

### **4. Duplicated logic is hard to test once**

The same badge ternary appears in `MeetingCard` and `MeetingTable`. If you extract it, you test one place; if you don’t, you duplicate tests or accept incomplete coverage.

### **5. Backend endpoint logic isn’t callable from unit tests**

In `RegistrationsEndpoints.cs`, the handler is an **inline lambda** inside `MapPost`. There’s no public method to invoke — so a “unit test” isn’t really an option without `WebApplicationFactory` + HTTP calls (integration test territory).

### **6. Validation doesn’t exist yet — so tests have nothing to assert**

The endpoint always returns `201 Created`. It never checks `meeting.Status` or venue capacity. Your three registration tests are skipped because the behavior you want to verify **isn’t implemented** — you’re testing a design, not working code.

### **7. Database and time are hard-coded dependencies**

The endpoint takes `MeetingFlowDbContext` directly and uses `DateTimeOffset.UtcNow`. That means:

- You can’t easily test without a database (or InMemory EF)
- You can’t assert exact timestamps (non-deterministic tests)
- There’s no `IClock` / `TimeProvider` to swap in

### **8. Integration tests need a lot of setup for simple rules**

To test “Draft meeting → rejected” today, you’d need: web host, HTTP client, seed data (Draft vs Published meetings, full vs empty venue), and still the validation code doesn’t exist. Heavy infrastructure for two `if` statements.

### **9. Page-level logic is mixed with fetching and rendering**

`MeetingDetailsPage` fetches data, handles loading/error states, *and* computes average rating. Testing the rating math through the page would mean mocking `fetchMeeting`, router params, and async state — way more than the calculation deserves.

### **10. Extraction made Part 5 easy — which highlights the pattern**

Once `formatAverageRating()` was pulled out, testing `[5,4,3] → "4.0"` took three lines and no DOM. The hard part wasn’t the math; it was realizing the logic **shouldn’t live in the component** in the first place.

We will review common patterns for making code testable, and you will see how
the same components can be restructured so the logic becomes trivial to test
without heavy mocking or infrastructure setup.

---



## Summary of deliverables


| #   | Task                                       | Time   | Required? |
| --- | ------------------------------------------ | ------ | --------- |
| 0   | Setup — run the app                        | 10 min | Yes       |
| 1   | Read the code, answer reflection questions | 15 min | Yes       |
| 2   | Set up Vitest + xUnit test project         | 10 min | Yes       |
| 3   | Test the badge logic in MeetingCard        | 20 min | Yes       |
| 4   | Test registration validation (sketch)      | 20 min | Yes       |
| 5   | Extract and test average rating            | 10 min | Bonus     |


**Total: ~75 minutes** (65 without the bonus)
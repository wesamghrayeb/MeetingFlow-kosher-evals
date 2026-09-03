import { describe, expect, it } from "vitest";
import { formatAverageRating } from "../averageRating";

describe("formatAverageRating", () => {
  it('returns "4.0" for ratings [5, 4, 3]', () => {
    expect(formatAverageRating([5, 4, 3])).toBe("4.0");
  });

  it('returns "N/A" for an empty array', () => {
    expect(formatAverageRating([])).toBe("N/A");
  });

  it('returns "1.0" for a single rating', () => {
    expect(formatAverageRating([1])).toBe("1.0");
  });
});

import {
  formatChartTime,
  formatDateTime,
  formatNumber,
  formatTime,
  formatTimestamp,
  parseTimestamp,
} from "@/lib/format";

describe("parseTimestamp", () => {
  it("returns the same Date instance when given a Date", () => {
    const date = new Date("2024-06-01T12:00:00Z");
    expect(parseTimestamp(date)).toBe(date);
  });

  it("parses UTC timestamps with an explicit offset", () => {
    const parsed = parseTimestamp("2024-06-01T12:00:00Z");
    expect(parsed.toISOString()).toBe("2024-06-01T12:00:00.000Z");
  });

  it("treats timezone-less ISO strings as UTC", () => {
    const parsed = parseTimestamp("2024-06-01T12:00:00");
    expect(parsed.toISOString()).toBe("2024-06-01T12:00:00.000Z");
  });

  it("treats date-only strings as UTC midnight", () => {
    const parsed = parseTimestamp("2024-06-01");
    expect(parsed.toISOString()).toBe("2024-06-01T00:00:00.000Z");
  });

  it("returns an invalid Date for empty strings", () => {
    expect(Number.isNaN(parseTimestamp("").getTime())).toBe(true);
  });
});

describe("format helpers", () => {
  const sample = "2024-06-01T12:34:56Z";

  it("formats timestamps for tables", () => {
    expect(formatTimestamp(sample)).toMatch(/2024/);
  });

  it("formats chart axis labels", () => {
    expect(formatChartTime(sample)).toMatch(/Jun/);
  });

  it("formats time-only labels", () => {
    expect(formatTime(sample)).toMatch(/\d/);
  });

  it("formats compact date-time labels", () => {
    expect(formatDateTime(sample)).toMatch(/Jun/);
  });

  it("returns the original string for invalid timestamps", () => {
    expect(formatTimestamp("not-a-date")).toBe("not-a-date");
  });

  it("returns an em dash for invalid Date objects", () => {
    expect(formatTimestamp(new Date(Number.NaN))).toBe("—");
  });

  it("falls back when formatting throws", () => {
    vi.spyOn(Date.prototype, "toLocaleString").mockImplementation(() => {
      throw new Error("locale failure");
    });

    expect(formatTimestamp("2024-06-01T12:00:00Z")).toBe(
      "2024-06-01T12:00:00Z",
    );
  });
});

describe("formatNumber", () => {
  it("returns an em dash for nullish values", () => {
    expect(formatNumber(null)).toBe("—");
    expect(formatNumber(undefined)).toBe("—");
  });

  it("formats numbers with the requested precision", () => {
    expect(formatNumber(12.345, 2)).toBe("12.35");
  });
});

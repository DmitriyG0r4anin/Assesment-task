const LOCALE =
  typeof navigator !== "undefined" ? navigator.language : undefined;

/** Parses API timestamps (UTC) and returns a Date in the user's local timezone. */
export function parseTimestamp(value: string | Date): Date {
  if (value instanceof Date) return value;

  const trimmed = value.trim();
  if (!trimmed) return new Date(Number.NaN);

  if (/([Zz]|[+-]\d{2}:?\d{2})$/.test(trimmed)) {
    return new Date(trimmed);
  }

  const normalized = trimmed.includes("T")
    ? `${trimmed}Z`
    : `${trimmed}T00:00:00Z`;
  return new Date(normalized);
}

function formatWithFallback(
  value: string | Date,
  formatter: (date: Date) => string,
): string {
  try {
    const date = parseTimestamp(value);
    if (Number.isNaN(date.getTime())) {
      return typeof value === "string" ? value : "—";
    }
    return formatter(date);
  } catch {
    return typeof value === "string" ? value : "—";
  }
}

export function formatTimestamp(ts: string | Date): string {
  return formatWithFallback(ts, (date) =>
    date.toLocaleString(LOCALE, {
      year: "numeric",
      month: "short",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
    }),
  );
}

export function formatChartTime(ts: string | Date): string {
  return formatWithFallback(ts, (date) =>
    date.toLocaleString(LOCALE, {
      month: "short",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    }),
  );
}

export function formatTime(ts: string | Date): string {
  return formatWithFallback(ts, (date) =>
    date.toLocaleTimeString(LOCALE, {
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
    }),
  );
}

export function formatDateTime(ts: string | Date): string {
  return formatWithFallback(ts, (date) =>
    date.toLocaleString(LOCALE, {
      month: "short",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
    }),
  );
}

export function formatNumber(
  value: number | null | undefined,
  decimals = 1,
): string {
  if (value == null) return "—";
  return value.toFixed(decimals);
}

import type { ChartTimeRangeMinutes, RefreshIntervalUnit } from "../lib/metrics";

/** Base palette (Tailwind default scale hex values). */
export const COLORS = {
  blue600: "#2563eb",
  emerald600: "#059669",
  amber600: "#d97706",
  violet600: "#7c3aed",
  slate200: "#e2e8f0",
  slate500: "#64748b",
};

export const CHART_COLORS = {
  co2: COLORS.blue600,
  pm25: COLORS.emerald600,
  humidity: COLORS.amber600,
  energy: COLORS.violet600,
};

/** Recharts chrome colors (aligned with Tailwind slate palette). */
export const CHART_UI_COLORS = {
  grid: COLORS.slate200,
  axis: COLORS.slate500,
  tooltipBorder: COLORS.slate200,
};
export const CHART_TOOLTIP_STYLE = {
  borderRadius: "8px",
  border: `1px solid ${CHART_UI_COLORS.tooltipBorder}`,
};

export const METRIC_UNITS = {
  co2: " ppm",
  pm25: " µg/m³",
  humidity: "%",
  energy: " kWh",
};

export const METRIC_LABELS = {
  co2: "CO₂",
  pm25: "PM2.5",
  humidity: "Humidity",
  energy: "Energy",
};

export const AIR_METRIC_DATA_KEYS = {
  co2: "avgCo2",
  pm25: "avgPm25",
  humidity: "avgHumidity",
} as const;

export type AirMetricKey =
  (typeof AIR_METRIC_DATA_KEYS)[keyof typeof AIR_METRIC_DATA_KEYS];

export const DASHBOARD_LIMITS = {
  sensorFetch: 100,
  sensorPoll: 25,
  tableRows: 12,
  energyChartDecimals: 3,
  energyCardDecimals: 2,
};

export const CHART_TIME_RANGES: ReadonlyArray<{
  label: string;
  minutes: ChartTimeRangeMinutes;
}> = [
  { label: "Last 5 minutes", minutes: 5 },
  { label: "Last 15 minutes", minutes: 15 },
  { label: "Last 30 minutes", minutes: 30 },
  { label: "All time", minutes: null },
];

export const REFRESH_INTERVAL_UNITS: ReadonlyArray<{
  value: RefreshIntervalUnit;
  label: string;
}> = [
  { value: "seconds", label: "Seconds" },
  { value: "minutes", label: "Minutes" },
  { value: "hours", label: "Hours" },
];

export const AIR_METRIC_CHARTS = [
  {
    suffix: "co2",
    title: `${METRIC_LABELS.co2} (ppm)`,
    dataKey: AIR_METRIC_DATA_KEYS.co2,
    seriesName: METRIC_LABELS.co2,
    stroke: CHART_COLORS.co2,
    unit: METRIC_UNITS.co2,
  },
  {
    suffix: "pm25",
    title: `${METRIC_LABELS.pm25} (µg/m³)`,
    dataKey: AIR_METRIC_DATA_KEYS.pm25,
    seriesName: METRIC_LABELS.pm25,
    stroke: CHART_COLORS.pm25,
    unit: METRIC_UNITS.pm25,
  },
  {
    suffix: "humidity",
    title: `${METRIC_LABELS.humidity} (%)`,
    dataKey: AIR_METRIC_DATA_KEYS.humidity,
    seriesName: METRIC_LABELS.humidity,
    stroke: CHART_COLORS.humidity,
    unit: METRIC_UNITS.humidity,
  },
] as const;

export const ENERGY_CHART = {
  suffix: "energy",
  title: `${METRIC_LABELS.energy} (kWh)`,
  seriesName: METRIC_LABELS.energy,
  stroke: CHART_COLORS.energy,
  unit: METRIC_UNITS.energy,
  decimals: DASHBOARD_LIMITS.energyChartDecimals,
};

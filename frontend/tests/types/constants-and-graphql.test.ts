import {
  AIR_METRIC_CHARTS,
  CHART_TIME_RANGES,
  DASHBOARD_LIMITS,
  ENERGY_CHART,
  METRIC_LABELS,
  REFRESH_INTERVAL_UNITS,
} from "@/types/constants";
import {
  AGGREGATE_BY_ROOM,
  AIR_QUERY,
  ENERGY_QUERY,
  MOTIONS_QUERY,
  ROOMS_QUERY,
} from "@/types/graphql";

describe("constants", () => {
  it("exposes dashboard limits and chart metadata", () => {
    expect(DASHBOARD_LIMITS.sensorFetch).toBe(100);
    expect(CHART_TIME_RANGES).toHaveLength(4);
    expect(AIR_METRIC_CHARTS.every((chart) => chart.title.length > 0)).toBe(
      true,
    );
    expect(ENERGY_CHART.seriesName).toBe(METRIC_LABELS.energy);
    expect(REFRESH_INTERVAL_UNITS.map((unit) => unit.value)).toEqual([
      "seconds",
      "minutes",
      "hours",
    ]);
  });
});

describe("graphql queries", () => {
  it("defines the expected operation names", () => {
    expect(String(ROOMS_QUERY)).toContain("RoomsForFilter");
    expect(String(AIR_QUERY)).toContain("AirQualities");
    expect(String(ENERGY_QUERY)).toContain("Energies");
    expect(String(MOTIONS_QUERY)).toContain("Motions");
    expect(String(AGGREGATE_BY_ROOM)).toContain("AggregateByRoom");
  });
});

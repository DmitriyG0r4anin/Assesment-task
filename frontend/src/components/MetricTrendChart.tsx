import { memo } from "react";
import {
  CartesianGrid,
  Line,
  LineChart,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { ChartContainer } from "./ChartContainer";
import { CHART_TOOLTIP_STYLE, CHART_UI_COLORS } from "../types/constants";

export type TrendPoint = {
  time: string;
  bucketKey: string;
  avgCo2: number | null;
  avgPm25: number | null;
  avgHumidity: number | null;
  motionCount: number;
};

export type SeriesTrendPoint = {
  time: string;
  bucketKey: string;
  value: number | null;
};

type AirMetricKey = keyof Pick<TrendPoint, "avgCo2" | "avgPm25" | "avgHumidity">;

export const MetricTrendChart = memo(function MetricTrendChart({
  title,
  data,
  dataKey,
  seriesName,
  stroke,
  unit,
  layoutKey,
}: {
  title: string;
  data: TrendPoint[];
  dataKey: AirMetricKey;
  seriesName: string;
  stroke: string;
  unit: string;
  layoutKey: string;
}) {
  return (
    <div className="min-w-0 w-full max-w-full overflow-hidden rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
      <h3 className="mb-4 text-sm font-medium text-slate-700">{title}</h3>
      <ChartContainer>
        {({ width, height }) => (
          <LineChart
            key={layoutKey}
            width={width}
            height={height}
            data={data}
            margin={{ top: 8, right: 8, left: 4, bottom: 0 }}
            style={{ maxWidth: "100%" }}
          >
            <CartesianGrid strokeDasharray="3 3" stroke={CHART_UI_COLORS.grid} />
            <XAxis
              dataKey="time"
              tick={{ fontSize: 11 }}
              stroke={CHART_UI_COLORS.axis}
              minTickGap={24}
              interval="preserveStartEnd"
            />
            <YAxis tick={{ fontSize: 11 }} stroke={CHART_UI_COLORS.axis} unit={unit} />
            <Tooltip contentStyle={CHART_TOOLTIP_STYLE} />
            <Line
              type="monotone"
              dataKey={dataKey}
              name={seriesName}
              stroke={stroke}
              strokeWidth={2}
              dot={false}
              connectNulls
              isAnimationActive={false}
            />
          </LineChart>
        )}
      </ChartContainer>
    </div>
  );
});

export const SeriesTrendChart = memo(function SeriesTrendChart({
  title,
  data,
  seriesName,
  stroke,
  unit,
  layoutKey,
  decimals = 1,
}: {
  title: string;
  data: SeriesTrendPoint[];
  seriesName: string;
  stroke: string;
  unit: string;
  layoutKey: string;
  decimals?: number;
}) {
  return (
    <div className="min-w-0 w-full max-w-full overflow-hidden rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
      <h3 className="mb-4 text-sm font-medium text-slate-700">{title}</h3>
      <ChartContainer>
        {({ width, height }) => (
          <LineChart
            key={layoutKey}
            width={width}
            height={height}
            data={data}
            margin={{ top: 8, right: 8, left: 4, bottom: 0 }}
            style={{ maxWidth: "100%" }}
          >
            <CartesianGrid strokeDasharray="3 3" stroke={CHART_UI_COLORS.grid} />
            <XAxis
              dataKey="time"
              tick={{ fontSize: 11 }}
              stroke={CHART_UI_COLORS.axis}
              minTickGap={24}
              interval="preserveStartEnd"
            />
            <YAxis
              tick={{ fontSize: 11 }}
              stroke={CHART_UI_COLORS.axis}
              unit={unit}
              tickFormatter={(v: number) => v.toFixed(decimals)}
            />
            <Tooltip
              contentStyle={CHART_TOOLTIP_STYLE}
              formatter={(value) =>
                typeof value === "number" ? value.toFixed(decimals) : value
              }
            />
            <Line
              type="monotone"
              dataKey="value"
              name={seriesName}
              stroke={stroke}
              strokeWidth={2}
              dot={false}
              connectNulls
              isAnimationActive={false}
            />
          </LineChart>
        )}
      </ChartContainer>
    </div>
  );
});

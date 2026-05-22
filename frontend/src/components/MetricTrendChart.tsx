import {
  CartesianGrid,
  Line,
  LineChart,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { ChartContainer } from "./ChartContainer";

export type TrendPoint = {
  time: string;
  bucketKey: string;
  avgCo2: number | null;
  avgPm25: number | null;
  avgHumidity: number | null;
  motionCount: number;
};

type MetricKey = keyof Pick<TrendPoint, "avgCo2" | "avgPm25" | "avgHumidity">;

export function trendChartKey(
  revision: number,
  data: TrendPoint[],
  suffix: string,
): string {
  if (data.length === 0) return `${revision}-${suffix}-empty`;
  const last = data[data.length - 1];
  return `${revision}-${suffix}-${data.length}-${last.bucketKey}`;
}

export function MetricTrendChart({
  title,
  data,
  dataKey,
  seriesName,
  stroke,
  unit,
  chartKey,
}: {
  title: string;
  data: TrendPoint[];
  dataKey: MetricKey;
  seriesName: string;
  stroke: string;
  unit: string;
  chartKey: string;
}) {
  return (
    <div className="min-w-0 w-full max-w-full overflow-hidden rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
      <h3 className="mb-4 text-sm font-medium text-slate-700">{title}</h3>
      <ChartContainer>
        {({ width, height }) => (
          <LineChart
            key={chartKey}
            width={width}
            height={height}
            data={data}
            margin={{ top: 8, right: 8, left: 4, bottom: 0 }}
            style={{ maxWidth: "100%" }}
          >
            <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
            <XAxis
              dataKey="time"
              tick={{ fontSize: 11 }}
              stroke="#64748b"
              minTickGap={24}
              interval="preserveStartEnd"
            />
            <YAxis tick={{ fontSize: 11 }} stroke="#64748b" unit={unit} />
            <Tooltip
              contentStyle={{
                borderRadius: "8px",
                border: "1px solid #e2e8f0",
              }}
            />
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
}

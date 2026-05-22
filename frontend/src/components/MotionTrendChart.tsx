import {
  Bar,
  BarChart,
  CartesianGrid,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { ChartContainer } from "./ChartContainer";
import type { TrendPoint } from "./MetricTrendChart";

export function MotionTrendChart({
  data,
  chartKey,
  roomLabel,
}: {
  data: TrendPoint[];
  chartKey: string;
  roomLabel?: string;
}) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
      <h3 className="mb-4 text-sm font-medium text-slate-700">
        Motion events
        {roomLabel ? ` — ${roomLabel}` : ""}
      </h3>
      <ChartContainer>
        {({ width, height }) => (
          <BarChart
            key={chartKey}
            width={width}
            height={height}
            data={data}
            margin={{ top: 8, right: 12, left: 0, bottom: 0 }}
          >
            <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
            <XAxis dataKey="time" tick={{ fontSize: 11 }} stroke="#64748b" />
            <YAxis allowDecimals={false} tick={{ fontSize: 11 }} stroke="#64748b" />
            <Tooltip
              contentStyle={{
                borderRadius: "8px",
                border: "1px solid #e2e8f0",
              }}
            />
            <Bar
              dataKey="motionCount"
              name="Detected"
              fill="#7c3aed"
              radius={[4, 4, 0, 0]}
              isAnimationActive={false}
            />
          </BarChart>
        )}
      </ChartContainer>
    </div>
  );
}

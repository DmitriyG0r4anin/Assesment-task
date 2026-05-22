import type { ReactNode } from "react";

function SkeletonBar({ className = "" }: { className?: string }) {
  return (
    <div
      className={`animate-pulse rounded-md bg-slate-200/90 ${className}`}
      aria-hidden
    />
  );
}

export function ChartCardSkeleton() {
  return (
    <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
      <SkeletonBar className="mb-4 h-4 w-32" />
      <SkeletonBar className="h-64 w-full" />
    </div>
  );
}

export type ChartsLayout = "grid" | "list";

export function chartsLayoutClassName(layout: ChartsLayout): string {
  return layout === "grid"
    ? "grid min-w-0 w-full max-w-full grid-cols-1 gap-6 xl:grid-cols-3"
    : "flex min-w-0 w-full max-w-full flex-col gap-6";
}

export function ChartsGridSkeleton({ layout = "grid" }: { layout?: ChartsLayout }) {
  return (
    <div className={chartsLayoutClassName(layout)}>
      <ChartCardSkeleton />
      <ChartCardSkeleton />
      <ChartCardSkeleton />
    </div>
  );
}

export function RoomPillsSkeleton() {
  return (
    <div className="flex flex-wrap gap-2">
      {Array.from({ length: 5 }).map((_, i) => (
        <SkeletonBar key={i} className="h-9 w-24 rounded-full" />
      ))}
    </div>
  );
}

export function RoomCardsSkeleton() {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {Array.from({ length: 6 }).map((_, i) => (
        <div
          key={i}
          className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm"
        >
          <SkeletonBar className="mb-3 h-5 w-28" />
          <div className="grid grid-cols-2 gap-2">
            {Array.from({ length: 6 }).map((__, j) => (
              <SkeletonBar key={j} className="h-10 w-full" />
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}

export function TableSkeleton({ rows = 6 }: { rows?: number }) {
  return (
    <div className="overflow-hidden rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
      <div className="mb-3 flex gap-4 border-b border-slate-100 pb-3">
        {Array.from({ length: 5 }).map((_, i) => (
          <SkeletonBar key={i} className="h-4 flex-1" />
        ))}
      </div>
      <div className="space-y-3">
        {Array.from({ length: rows }).map((_, i) => (
          <SkeletonBar key={i} className="h-8 w-full" />
        ))}
      </div>
    </div>
  );
}

export function DashboardBootstrapSkeleton() {
  return (
    <div className="space-y-10" aria-busy="true" aria-label="Loading dashboard">
      <div className="space-y-2">
        <SkeletonBar className="h-8 w-48" />
        <SkeletonBar className="h-4 w-72" />
      </div>
      <section className="space-y-4">
        <SkeletonBar className="h-6 w-56" />
        <div className="flex flex-wrap gap-4">
          <SkeletonBar className="h-16 w-40" />
          <SkeletonBar className="h-16 w-36" />
          <SkeletonBar className="h-16 w-48" />
        </div>
        <RoomPillsSkeleton />
        <ChartsGridSkeleton />
        <ChartCardSkeleton />
      </section>
      <section className="space-y-4">
        <SkeletonBar className="h-6 w-40" />
        <RoomCardsSkeleton />
      </section>
      <section className="space-y-4">
        <SkeletonBar className="h-6 w-44" />
        <TableSkeleton />
      </section>
    </div>
  );
}

/** Keeps layout stable: previous content stays sized; skeleton overlays while refetching. */
export function ContentLoadingOverlay({
  loading,
  skeleton,
  children,
  minHeight = "min-h-[12rem]",
}: {
  loading: boolean;
  skeleton: ReactNode;
  children: ReactNode;
  minHeight?: string;
}) {
  return (
    <div className={`relative min-w-0 w-full max-w-full ${minHeight}`}>
      <div
        className={
          loading
            ? "pointer-events-none invisible min-w-0 w-full max-w-full"
            : "min-w-0 w-full max-w-full transition-opacity duration-200"
        }
        aria-hidden={loading}
      >
        {children}
      </div>
      {loading && (
        <div
          className="absolute inset-0 z-10 rounded-xl bg-slate-50/90"
          aria-busy="true"
          aria-live="polite"
        >
          {skeleton}
        </div>
      )}
    </div>
  );
}

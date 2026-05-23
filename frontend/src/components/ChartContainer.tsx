import { memo, useEffect, useRef, useState, type ReactNode } from 'react';

const DEFAULT_HEIGHT = 256;

export const ChartContainer = memo(function ChartContainer({
    children,
    height = DEFAULT_HEIGHT,
}: {
    children: (size: { width: number; height: number }) => ReactNode;
    height?: number;
}) {
    const ref = useRef<HTMLDivElement>(null);
    const [width, setWidth] = useState(0);

    useEffect(() => {
        const el = ref.current;
        if (!el) return;

        const measure = () => {
            const next = Math.floor(el.clientWidth);
            if (next <= 0) return;
            setWidth((prev) => (prev === next ? prev : next));
        };

        measure();
        const observer = new ResizeObserver(() => measure());
        observer.observe(el);
        return () => observer.disconnect();
    }, []);

    return (
        <div ref={ref} className="w-full min-w-0 max-w-full overflow-hidden" style={{ height }}>
            {width > 0 ? <div className="max-w-full overflow-hidden">{children({ width, height })}</div> : null}
        </div>
    );
});

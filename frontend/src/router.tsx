import { lazy, Suspense, type ReactNode } from 'react';
import { createBrowserRouter } from 'react-router';
import { Layout } from './components/Layout';
import { DashboardBootstrapSkeleton } from './components/DashboardSkeleton';

const Dashboard = lazy(() => import('./pages/Dashboard').then(({ Dashboard }) => ({ default: Dashboard })));
const Parameters = lazy(() =>
    import('./pages/Parameters').then(({ Parameters }) => ({
        default: Parameters,
    })),
);
const Motion = lazy(() => import('./pages/Motion').then(({ Motion }) => ({ default: Motion })));

function PageFallback() {
    return (
        <div className="flex items-center justify-center gap-3 py-20 text-slate-500">
            <span
                className="size-5 animate-spin rounded-full border-2 border-slate-200 border-t-blue-600"
                aria-hidden
            />
            <span>Loading…</span>
        </div>
    );
}

function SuspenseRoute({ children }: Readonly<{ children: ReactNode }>) {
    return <Suspense fallback={<PageFallback />}>{children}</Suspense>;
}

export const router = createBrowserRouter([
    {
        path: '/',
        element: <Layout />,
        children: [
            {
                index: true,
                element: (
                    <Suspense fallback={<DashboardBootstrapSkeleton />}>
                        <Dashboard />
                    </Suspense>
                ),
            },
            {
                path: 'parameters',
                element: (
                    <SuspenseRoute>
                        <Parameters />
                    </SuspenseRoute>
                ),
            },
            {
                path: 'motion',
                element: (
                    <SuspenseRoute>
                        <Motion />
                    </SuspenseRoute>
                ),
            },
        ],
    },
]);

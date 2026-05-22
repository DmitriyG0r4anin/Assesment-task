import { Outlet, Link, useLocation } from "react-router";

const navItems = [
  { path: "/", label: "Dashboard" },
  { path: "/parameters", label: "Sensor data" },
  { path: "/motion", label: "Motion tracker" },
];

export function Layout() {
  const location = useLocation();

  const isActive = (path: string): boolean => {
    if (path === "/") return location.pathname === "/";
    return location.pathname.startsWith(path);
  };

  return (
    <div className="flex min-h-screen flex-col bg-slate-100 text-slate-900">
      <nav className="sticky top-0 z-50 border-b border-slate-200 bg-white shadow-sm">
        <div className="mx-auto flex h-14 max-w-6xl items-center justify-between px-4 sm:px-6">
          <Link
            to="/"
            className="text-lg font-semibold text-blue-600 transition hover:text-blue-700"
          >
            Environment monitor
          </Link>
          <ul className="flex gap-1">
            {navItems.map((item) => (
              <li key={item.path}>
                <Link
                  to={item.path}
                  className={`rounded-lg px-3 py-2 text-sm font-medium transition ${
                    isActive(item.path)
                      ? "bg-blue-50 text-blue-700"
                      : "text-slate-600 hover:bg-slate-50 hover:text-slate-900"
                  }`}
                >
                  {item.label}
                </Link>
              </li>
            ))}
          </ul>
        </div>
      </nav>
      <main className="mx-auto w-full min-w-0 max-w-6xl flex-1 px-4 py-8 sm:px-6">
        <Outlet />
      </main>
    </div>
  );
}

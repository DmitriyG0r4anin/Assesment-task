import { createBrowserRouter } from "react-router";
import { Layout } from "./components/Layout";
import { Dashboard } from "./pages/Dashboard";
import { Parameters } from "./pages/Parameters";
import { Motion } from "./pages/Motion";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <Layout />,
    children: [
      {
        index: true,
        element: <Dashboard />,
      },
      {
        path: "parameters",
        element: <Parameters />,
      },
      {
        path: "motion",
        element: <Motion />,
      },
    ],
  },
]);

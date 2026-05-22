import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { RouterProvider } from "react-router";
import { Toaster } from "sonner";
import { NotificationsProvider } from "./context/NotificationsContext";
import { router } from "./router";
import "./index.css";

const rootElement = document.getElementById("root");

if (!rootElement) {
  throw new Error("Root element not found");
}

createRoot(rootElement).render(
  <StrictMode>
    <NotificationsProvider>
      <RouterProvider router={router} />
      <Toaster richColors position="bottom-right" closeButton />
    </NotificationsProvider>
  </StrictMode>,
);

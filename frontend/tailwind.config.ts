import type { Config } from "tailwindcss";

export default {
  content: ["./index.html", "./src/**/*.{js,ts,jsx,tsx}"],
  theme: {
    extend: {
      maxHeight: {
        scroll: "520px",
      },
      minHeight: {
        "section-xs": "2.5rem",
        "section-sm": "12rem",
        "section-md": "14rem",
        "section-lg": "16rem",
        "section-xl": "24rem",
      },
      minWidth: {
        "select-time": "10rem",
        "select-unit": "7rem",
        "table-air": "640px",
        "table-default": "520px",
      },
      boxShadow: {
        "status-connected": "0 0 8px rgba(16, 185, 129, 0.6)",
        "status-disconnected": "0 0 8px rgba(239, 68, 68, 0.5)",
      },
    },
  },
} satisfies Config;

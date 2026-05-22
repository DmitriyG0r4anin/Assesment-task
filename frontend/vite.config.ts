import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 3000,
    // proxy: {
    //   "/graphql": {
    //     target: "http://localhost:4000",
    //     changeOrigin: true,
    //   },
    //   "/notifications": {
    //     target: "http://localhost:8092",
    //     changeOrigin: true,
    //   },
    // },
  },
});

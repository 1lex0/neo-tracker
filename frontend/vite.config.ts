import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// The browser only talks to same-origin /api; Vite forwards it to the ASP.NET Core API.
const apiTarget = process.env.NEO_API_URL ?? 'http://localhost:5080'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: apiTarget,
        changeOrigin: true,
      },
    },
  },
})

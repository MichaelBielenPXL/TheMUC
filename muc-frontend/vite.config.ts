import { fileURLToPath } from 'node:url'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Aspire sets these when it starts the dev server; the fallbacks are for a plain `npm run dev`.
const backendUrl =
  process.env.services__backend__http__0 ?? 'http://localhost:5246'
const port = process.env.PORT ? Number(process.env.PORT) : undefined

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  server: {
    port,
    proxy: {
      '/api': { target: backendUrl, changeOrigin: true },
    },
  },
})

import fs from 'node:fs'
import path from 'path'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// Serve the dev server over HTTPS using the exported ASP.NET Core dev certificate.
// Falls back to HTTP (with a hint) if the certs are missing, so a fresh checkout still starts.
function devHttps() {
  const key = path.resolve(__dirname, './certs/localhost.key')
  const cert = path.resolve(__dirname, './certs/localhost.pem')
  if (fs.existsSync(key) && fs.existsSync(cert)) {
    return { key: fs.readFileSync(key), cert: fs.readFileSync(cert) }
  }
  console.warn('[vite] ./certs not found — run `npm run certs`, then restart. Serving HTTP for now.')
  return undefined
}

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 5173,
    https: devHttps(),
    proxy: {
      '/api': {
        target: 'https://localhost:7233',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})

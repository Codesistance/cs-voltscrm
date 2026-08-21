import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { AppProviders } from './app/AppProviders'
import { registerServiceWorker } from './app/registerServiceWorker'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AppProviders>
      <App />
    </AppProviders>
  </StrictMode>,
)

if (import.meta.env.PROD) {
  registerServiceWorker()
}

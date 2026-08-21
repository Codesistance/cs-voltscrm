import { toast } from 'sonner'

export function registerServiceWorker() {
  if (!('serviceWorker' in navigator)) return

  window.addEventListener('load', () => {
    navigator.serviceWorker
      .register('/sw.js')
      .then((registration) => {
        registration.addEventListener('updatefound', () => {
          const installing = registration.installing
          if (!installing) return

          installing.addEventListener('statechange', () => {
            if (installing.state === 'installed' && navigator.serviceWorker.controller) {
              toast('A new version of Volts CRM is available', {
                action: {
                  label: 'Reload',
                  onClick: () => installing.postMessage('SKIP_WAITING'),
                },
                duration: Infinity,
              })
            }
          })
        })
      })
      .catch((error) => console.error('Service worker registration failed', error))
  })

  let reloading = false
  navigator.serviceWorker.addEventListener('controllerchange', () => {
    if (reloading) return
    reloading = true
    window.location.reload()
  })
}

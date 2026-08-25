/**
 * Copy text to the clipboard, working over plain HTTP as well as HTTPS.
 *
 * The async Clipboard API (`navigator.clipboard`) only exists in secure contexts — HTTPS or
 * localhost. When the app is served over plain HTTP (e.g. the S3 website endpoint in the
 * auto-FQDN topology), `navigator.clipboard` is `undefined`, so calling it throws and the copy
 * silently fails. This falls back to the legacy hidden-textarea `execCommand('copy')` path there.
 *
 * @returns true if the text was copied, false if every strategy failed.
 */
export async function copyText(text: string): Promise<boolean> {
  if (typeof navigator !== 'undefined' && navigator.clipboard && window.isSecureContext) {
    try {
      await navigator.clipboard.writeText(text)
      return true
    } catch {
      // Permission denied or blocked — fall through to the legacy path.
    }
  }
  return legacyCopy(text)
}

function legacyCopy(text: string): boolean {
  try {
    const textarea = document.createElement('textarea')
    textarea.value = text
    textarea.setAttribute('readonly', '')
    // Keep it out of view and prevent scroll/zoom jumps while it's briefly focused.
    textarea.style.position = 'fixed'
    textarea.style.top = '-9999px'
    textarea.style.opacity = '0'
    document.body.appendChild(textarea)
    textarea.focus()
    textarea.select()
    const ok = document.execCommand('copy')
    document.body.removeChild(textarea)
    return ok
  } catch {
    return false
  }
}

// Access token lives in memory only — never touches localStorage (XSS safe).
// The refresh token is stored in an httpOnly SameSite=Strict cookie by the server.
let accessToken: string | null = null

export const tokenStore = {
  get: (): string | null => accessToken,
  set: (token: string): void => { accessToken = token },
  clear: (): void => { accessToken = null },
}

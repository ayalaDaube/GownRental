// Keeps the signed-in session (the JWT and who it belongs to) in sessionStorage:
// it survives a page refresh but disappears when the browser tab is closed.

const STORAGE_KEY = 'gownrental.session';

export interface Session {
  token: string;
  username: string;
  role: string;
  expiresAtUtc: string;
}

export function saveSession(session: Session): void {
  try {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
  } catch {
    // Storage can be unavailable (private mode); the session then lasts until the page is reloaded.
  }
}

export function clearSession(): void {
  try {
    sessionStorage.removeItem(STORAGE_KEY);
  } catch {
    // ignore
  }
}

/** Returns the stored session, or null when there is none or it has expired. */
export function loadSession(): Session | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) return null;

    const session = JSON.parse(raw) as Session;
    if (!session.token || new Date(session.expiresAtUtc).getTime() <= Date.now()) {
      clearSession();
      return null;
    }
    return session;
  } catch {
    return null;
  }
}

export function getToken(): string | null {
  return loadSession()?.token ?? null;
}

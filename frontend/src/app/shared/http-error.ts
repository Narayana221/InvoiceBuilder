import { HttpErrorResponse } from '@angular/common/http';

export function extractErrorMessage(err: unknown, fallback: string): string {
  if (err instanceof HttpErrorResponse && err.error?.errors) {
    const messages = Object.values(err.error.errors as Record<string, string[]>).flat();
    if (messages.length) return messages.join(' ');
  }
  return fallback;
}

/** For requests made with `responseType: 'blob'`, the error body also arrives as a Blob, not parsed JSON. */
export async function extractBlobErrorMessage(err: unknown, fallback: string): Promise<string> {
  if (err instanceof HttpErrorResponse && err.error instanceof Blob) {
    try {
      const parsed = JSON.parse(await err.error.text());
      return parsed.detail || parsed.title || fallback;
    } catch {
      return fallback;
    }
  }
  return fallback;
}

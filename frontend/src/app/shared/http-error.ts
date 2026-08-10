import { HttpErrorResponse } from '@angular/common/http';

export function extractErrorMessage(err: unknown, fallback: string): string {
  if (err instanceof HttpErrorResponse && err.error?.errors) {
    const messages = Object.values(err.error.errors as Record<string, string[]>).flat();
    if (messages.length) return messages.join(' ');
  }
  return fallback;
}

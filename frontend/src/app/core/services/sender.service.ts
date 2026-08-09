import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Sender, SenderRequest } from '../models/sender.model';
import { PagedResult } from '../models/paged-result.model';

@Injectable({ providedIn: 'root' })
export class SenderService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/senders`;

  private readonly _senders = signal<Sender[]>([]);
  private readonly _totalCount = signal(0);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly senders = this._senders.asReadonly();
  readonly totalCount = this._totalCount.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  list(page = 1, pageSize = 20): void {
    this._loading.set(true);
    this._error.set(null);

    this.http
      .get<PagedResult<Sender>>(this.baseUrl, { params: { page, pageSize } })
      .subscribe({
        next: (result) => {
          this._senders.set(result.items);
          this._totalCount.set(result.totalCount);
          this._loading.set(false);
        },
        error: () => {
          this._error.set('Failed to load senders.');
          this._loading.set(false);
        },
      });
  }

  get(id: string) {
    return this.http.get<Sender>(`${this.baseUrl}/${id}`);
  }

  create(request: SenderRequest) {
    return this.http.post<Sender>(this.baseUrl, request);
  }

  update(id: string, request: SenderRequest) {
    return this.http.put<Sender>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string) {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}

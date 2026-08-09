import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Invoice, InvoiceRequest, InvoiceSummary } from '../models/invoice.model';
import { PagedResult } from '../models/paged-result.model';

@Injectable({ providedIn: 'root' })
export class InvoiceService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/invoices`;

  private readonly _invoices = signal<InvoiceSummary[]>([]);
  private readonly _totalCount = signal(0);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly invoices = this._invoices.asReadonly();
  readonly totalCount = this._totalCount.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  list(page = 1, pageSize = 20): void {
    this._loading.set(true);
    this._error.set(null);

    this.http
      .get<PagedResult<InvoiceSummary>>(this.baseUrl, { params: { page, pageSize } })
      .subscribe({
        next: (result) => {
          this._invoices.set(result.items);
          this._totalCount.set(result.totalCount);
          this._loading.set(false);
        },
        error: () => {
          this._error.set('Failed to load invoices.');
          this._loading.set(false);
        },
      });
  }

  get(id: string) {
    return this.http.get<Invoice>(`${this.baseUrl}/${id}`);
  }

  create(request: InvoiceRequest) {
    return this.http.post<Invoice>(this.baseUrl, request);
  }

  update(id: string, request: InvoiceRequest) {
    return this.http.put<Invoice>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string) {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  downloadPdf(id: string) {
    return this.http.get(`${this.baseUrl}/${id}/pdf`, { responseType: 'blob' });
  }
}

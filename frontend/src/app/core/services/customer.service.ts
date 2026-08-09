import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Customer, CustomerRequest } from '../models/customer.model';
import { PagedResult } from '../models/paged-result.model';

@Injectable({ providedIn: 'root' })
export class CustomerService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/customers`;

  private readonly _customers = signal<Customer[]>([]);
  private readonly _totalCount = signal(0);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly customers = this._customers.asReadonly();
  readonly totalCount = this._totalCount.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  list(page = 1, pageSize = 20): void {
    this._loading.set(true);
    this._error.set(null);

    this.http
      .get<PagedResult<Customer>>(this.baseUrl, { params: { page, pageSize } })
      .subscribe({
        next: (result) => {
          this._customers.set(result.items);
          this._totalCount.set(result.totalCount);
          this._loading.set(false);
        },
        error: () => {
          this._error.set('Failed to load customers.');
          this._loading.set(false);
        },
      });
  }

  get(id: string) {
    return this.http.get<Customer>(`${this.baseUrl}/${id}`);
  }

  create(request: CustomerRequest) {
    return this.http.post<Customer>(this.baseUrl, request);
  }

  update(id: string, request: CustomerRequest) {
    return this.http.put<Customer>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string) {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}

import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CustomerService } from '../core/services/customer.service';
import { ConfirmDialog } from '../shared/confirm-dialog';
import { Pager } from '../shared/pager';

const PAGE_SIZE = 20;

@Component({
  selector: 'app-customers-page',
  imports: [RouterLink, ConfirmDialog, Pager],
  templateUrl: './customers-page.html',
})
export class CustomersPage implements OnInit {
  private readonly customerService = inject(CustomerService);

  readonly customers = this.customerService.customers;
  readonly loading = this.customerService.loading;
  readonly error = this.customerService.error;
  readonly totalCount = this.customerService.totalCount;
  readonly pageSize = PAGE_SIZE;

  readonly page = signal(1);
  readonly pendingDeleteId = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.customerService.list(this.page(), this.pageSize);
  }

  onPageChange(page: number): void {
    this.page.set(page);
    this.load();
  }

  confirmDelete(id: string): void {
    this.pendingDeleteId.set(id);
  }

  cancelDelete(): void {
    this.pendingDeleteId.set(null);
  }

  deleteConfirmed(): void {
    const id = this.pendingDeleteId();
    if (!id) return;

    this.customerService.delete(id).subscribe({
      next: () => {
        this.pendingDeleteId.set(null);
        this.load();
      },
      error: () => this.pendingDeleteId.set(null),
    });
  }
}

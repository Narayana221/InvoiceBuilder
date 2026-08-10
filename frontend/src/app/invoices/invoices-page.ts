import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { InvoiceService } from '../core/services/invoice.service';
import { ConfirmDialog } from '../shared/confirm-dialog';
import { Pager } from '../shared/pager';
import { downloadBlob } from '../shared/download-blob';
import { extractBlobErrorMessage } from '../shared/http-error';
import type { InvoiceSummary } from '../core/models/invoice.model';

const PAGE_SIZE = 20;

@Component({
  selector: 'app-invoices-page',
  imports: [RouterLink, ConfirmDialog, Pager],
  templateUrl: './invoices-page.html',
})
export class InvoicesPage implements OnInit {
  private readonly invoiceService = inject(InvoiceService);

  readonly invoices = this.invoiceService.invoices;
  readonly loading = this.invoiceService.loading;
  readonly error = this.invoiceService.error;
  readonly totalCount = this.invoiceService.totalCount;
  readonly pageSize = PAGE_SIZE;

  readonly page = signal(1);
  readonly pendingDeleteId = signal<string | null>(null);
  readonly downloadingId = signal<string | null>(null);
  readonly downloadError = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.invoiceService.list(this.page(), this.pageSize);
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

    this.invoiceService.delete(id).subscribe({
      next: () => {
        this.pendingDeleteId.set(null);
        this.load();
      },
      error: () => this.pendingDeleteId.set(null),
    });
  }

  downloadPdf(invoice: InvoiceSummary): void {
    this.downloadingId.set(invoice.id);
    this.downloadError.set(null);

    this.invoiceService.downloadPdf(invoice.id).subscribe({
      next: (blob) => {
        downloadBlob(blob, `${invoice.invoiceNumber}.pdf`);
        this.downloadingId.set(null);
      },
      error: async (err) => {
        this.downloadError.set(await extractBlobErrorMessage(err, 'Failed to download PDF.'));
        this.downloadingId.set(null);
      },
    });
  }
}

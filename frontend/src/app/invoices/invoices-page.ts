import { Component, OnInit, inject } from '@angular/core';
import { InvoiceService } from '../core/services/invoice.service';

@Component({
  selector: 'app-invoices-page',
  imports: [],
  templateUrl: './invoices-page.html',
})
export class InvoicesPage implements OnInit {
  private readonly invoiceService = inject(InvoiceService);

  readonly invoices = this.invoiceService.invoices;
  readonly loading = this.invoiceService.loading;
  readonly error = this.invoiceService.error;
  readonly totalCount = this.invoiceService.totalCount;

  ngOnInit(): void {
    this.invoiceService.list();
  }
}

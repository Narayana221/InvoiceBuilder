import { Component, OnInit, inject } from '@angular/core';
import { CustomerService } from '../core/services/customer.service';

@Component({
  selector: 'app-customers-page',
  imports: [],
  templateUrl: './customers-page.html',
})
export class CustomersPage implements OnInit {
  private readonly customerService = inject(CustomerService);

  readonly customers = this.customerService.customers;
  readonly loading = this.customerService.loading;
  readonly error = this.customerService.error;
  readonly totalCount = this.customerService.totalCount;

  ngOnInit(): void {
    this.customerService.list();
  }
}

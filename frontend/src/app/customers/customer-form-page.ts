import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CustomerService } from '../core/services/customer.service';
import { extractErrorMessage } from '../shared/http-error';

@Component({
  selector: 'app-customer-form-page',
  imports: [ReactiveFormsModule],
  templateUrl: './customer-form-page.html',
})
export class CustomerFormPage implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly customerService = inject(CustomerService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  private customerId: string | null = null;
  readonly isEditMode = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    contactName: ['', [Validators.maxLength(200)]],
    addressLine: ['', [Validators.required, Validators.maxLength(300)]],
    city: ['', [Validators.required, Validators.maxLength(100)]],
    postalCode: ['', [Validators.maxLength(20)]],
    country: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.maxLength(200), Validators.email]],
    taxId: ['', [Validators.maxLength(50)]],
  });

  ngOnInit(): void {
    this.customerId = this.route.snapshot.paramMap.get('id');
    if (!this.customerId) return;

    this.isEditMode.set(true);
    this.customerService.get(this.customerId).subscribe({
      next: (customer) => {
        this.form.patchValue({
          name: customer.name,
          contactName: customer.contactName ?? '',
          addressLine: customer.addressLine,
          city: customer.city,
          postalCode: customer.postalCode ?? '',
          country: customer.country,
          email: customer.email ?? '',
          taxId: customer.taxId ?? '',
        });
      },
      error: () => this.error.set('Failed to load customer.'),
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const raw = this.form.getRawValue();
    const request = {
      name: raw.name,
      contactName: raw.contactName || null,
      addressLine: raw.addressLine,
      city: raw.city,
      postalCode: raw.postalCode || null,
      country: raw.country,
      email: raw.email || null,
      taxId: raw.taxId || null,
    };

    const result$ = this.customerId
      ? this.customerService.update(this.customerId, request)
      : this.customerService.create(request);

    result$.subscribe({
      next: () => this.router.navigate(['/customers']),
      error: (err) => {
        this.error.set(extractErrorMessage(err, 'Failed to save customer.'));
        this.saving.set(false);
      },
    });
  }

  cancel(): void {
    this.router.navigate(['/customers']);
  }
}

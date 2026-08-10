import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DecimalPipe } from '@angular/common';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CustomerService } from '../core/services/customer.service';
import { SenderService } from '../core/services/sender.service';
import { InvoiceService } from '../core/services/invoice.service';
import { extractErrorMessage } from '../shared/http-error';

function dueDateNotBeforeInvoiceDate(control: AbstractControl): ValidationErrors | null {
  const invoiceDate = control.get('invoiceDate')?.value;
  const dueDate = control.get('dueDate')?.value;
  if (!invoiceDate || !dueDate) return null;
  return dueDate < invoiceDate ? { dueDateBeforeInvoiceDate: true } : null;
}

@Component({
  selector: 'app-invoice-form-page',
  imports: [ReactiveFormsModule, DecimalPipe],
  templateUrl: './invoice-form-page.html',
})
export class InvoiceFormPage implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly invoiceService = inject(InvoiceService);
  private readonly customerService = inject(CustomerService);
  private readonly senderService = inject(SenderService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  private invoiceId: string | null = null;
  readonly isEditMode = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly customers = this.customerService.customers;
  readonly senders = this.senderService.senders;

  readonly form = this.fb.nonNullable.group(
    {
      invoiceDate: ['', Validators.required],
      dueDate: ['', Validators.required],
      customerId: ['', Validators.required],
      senderId: ['', Validators.required],
      currency: ['USD', [Validators.required, Validators.pattern(/^[A-Z]{3}$/)]],
      taxRatePercent: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
      notes: [''],
      lineItems: this.fb.array([this.createLineItem()]),
    },
    { validators: dueDateNotBeforeInvoiceDate },
  );

  private readonly formValue = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });

  readonly subtotal = computed(() => {
    const value = this.formValue();
    return (value.lineItems ?? []).reduce(
      (sum, item) => sum + (Number(item?.quantity) || 0) * (Number(item?.unitPrice) || 0),
      0,
    );
  });

  readonly taxAmount = computed(() => this.subtotal() * ((Number(this.formValue().taxRatePercent) || 0) / 100));

  readonly total = computed(() => this.subtotal() + this.taxAmount());

  get lineItems() {
    return this.form.controls.lineItems;
  }

  ngOnInit(): void {
    this.customerService.list(1, 100);
    this.senderService.list(1, 100);

    this.invoiceId = this.route.snapshot.paramMap.get('id');
    if (!this.invoiceId) return;

    this.isEditMode.set(true);
    this.invoiceService.get(this.invoiceId).subscribe({
      next: (invoice) => {
        this.lineItems.clear();
        invoice.lineItems.forEach((item) =>
          this.lineItems.push(this.createLineItem(item.description, item.quantity, item.unitPrice)),
        );
        this.form.patchValue({
          invoiceDate: invoice.invoiceDate,
          dueDate: invoice.dueDate,
          customerId: invoice.customerId,
          senderId: invoice.senderId,
          currency: invoice.currency,
          taxRatePercent: invoice.taxRatePercent,
          notes: invoice.notes ?? '',
        });
      },
      error: () => this.error.set('Failed to load invoice.'),
    });
  }

  private createLineItem(description = '', quantity = 1, unitPrice = 0) {
    return this.fb.nonNullable.group({
      description: [description, [Validators.required, Validators.maxLength(500)]],
      quantity: [quantity, [Validators.required, Validators.min(0.01)]],
      unitPrice: [unitPrice, [Validators.required, Validators.min(0)]],
    });
  }

  addLineItem(): void {
    this.lineItems.push(this.createLineItem());
  }

  removeLineItem(index: number): void {
    if (this.lineItems.length > 1) {
      this.lineItems.removeAt(index);
    }
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
      invoiceDate: raw.invoiceDate,
      dueDate: raw.dueDate,
      customerId: raw.customerId,
      senderId: raw.senderId,
      currency: raw.currency.toUpperCase(),
      taxRatePercent: raw.taxRatePercent,
      notes: raw.notes || null,
      lineItems: raw.lineItems,
    };

    const result$ = this.invoiceId
      ? this.invoiceService.update(this.invoiceId, request)
      : this.invoiceService.create(request);

    result$.subscribe({
      next: () => this.router.navigate(['/invoices']),
      error: (err) => {
        this.error.set(extractErrorMessage(err, 'Failed to save invoice.'));
        this.saving.set(false);
      },
    });
  }

  cancel(): void {
    this.router.navigate(['/invoices']);
  }
}

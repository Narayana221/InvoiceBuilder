import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { SenderService } from '../core/services/sender.service';
import { extractErrorMessage } from '../shared/http-error';

@Component({
  selector: 'app-sender-form-page',
  imports: [ReactiveFormsModule],
  templateUrl: './sender-form-page.html',
})
export class SenderFormPage implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly senderService = inject(SenderService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  private senderId: string | null = null;
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
    bankDetails: ['', [Validators.maxLength(200)]],
  });

  ngOnInit(): void {
    this.senderId = this.route.snapshot.paramMap.get('id');
    if (!this.senderId) return;

    this.isEditMode.set(true);
    this.senderService.get(this.senderId).subscribe({
      next: (sender) => {
        this.form.patchValue({
          name: sender.name,
          contactName: sender.contactName ?? '',
          addressLine: sender.addressLine,
          city: sender.city,
          postalCode: sender.postalCode ?? '',
          country: sender.country,
          email: sender.email ?? '',
          taxId: sender.taxId ?? '',
          bankDetails: sender.bankDetails ?? '',
        });
      },
      error: () => this.error.set('Failed to load sender.'),
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
      bankDetails: raw.bankDetails || null,
    };

    const result$ = this.senderId
      ? this.senderService.update(this.senderId, request)
      : this.senderService.create(request);

    result$.subscribe({
      next: () => this.router.navigate(['/senders']),
      error: (err) => {
        this.error.set(extractErrorMessage(err, 'Failed to save sender.'));
        this.saving.set(false);
      },
    });
  }

  cancel(): void {
    this.router.navigate(['/senders']);
  }
}

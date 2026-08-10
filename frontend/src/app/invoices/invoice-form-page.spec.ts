import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { InvoiceFormPage } from './invoice-form-page';
import { environment } from '../../environments/environment';

describe('InvoiceFormPage', () => {
  let fixture: ComponentFixture<InvoiceFormPage>;
  let component: InvoiceFormPage;
  let httpMock: HttpTestingController;

  const emptyPage = { items: [], page: 1, pageSize: 100, totalCount: 0, totalPages: 1 };

  function setup(routeId: string | null = null) {
    TestBed.configureTestingModule({
      imports: [InvoiceFormPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap(routeId ? { id: routeId } : {}) } },
        },
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(InvoiceFormPage);
    component = fixture.componentInstance;
    fixture.detectChanges();

    // ngOnInit always loads the customer/sender dropdowns, regardless of create vs. edit mode.
    httpMock.expectOne((r) => r.url === `${environment.apiBaseUrl}/api/customers`).flush(emptyPage);
    httpMock.expectOne((r) => r.url === `${environment.apiBaseUrl}/api/senders`).flush(emptyPage);
  }

  afterEach(() => {
    httpMock.verify();
  });

  describe('live totals', () => {
    it('computes subtotal, tax, and total from the line items and tax rate', () => {
      setup();

      component.lineItems.at(0).patchValue({ description: 'Widget', quantity: 2, unitPrice: 50 });
      component.addLineItem();
      component.lineItems.at(1).patchValue({ description: 'Gadget', quantity: 1, unitPrice: 25 });
      component.form.patchValue({ taxRatePercent: 20 });

      // subtotal = 2*50 + 1*25 = 125, tax = 125 * 0.20 = 25, total = 150
      expect(component.subtotal()).toBe(125);
      expect(component.taxAmount()).toBe(25);
      expect(component.total()).toBe(150);
    });

    it('recomputes when a line item is removed', () => {
      setup();

      component.lineItems.at(0).patchValue({ quantity: 2, unitPrice: 50 });
      component.addLineItem();
      component.lineItems.at(1).patchValue({ quantity: 1, unitPrice: 25 });
      expect(component.subtotal()).toBe(125);

      component.removeLineItem(1);
      expect(component.subtotal()).toBe(100);
    });
  });

  describe('due date validation (regression guard for the bug where an invalid date silently round-tripped to a 400)', () => {
    it('marks the form invalid when the due date is before the invoice date', () => {
      setup();

      component.form.patchValue({ invoiceDate: '2026-08-20', dueDate: '2026-08-10' });

      expect(component.form.hasError('dueDateBeforeInvoiceDate')).toBe(true);
      expect(component.form.invalid).toBe(true);
    });

    it('is valid when the due date is on or after the invoice date', () => {
      setup();

      component.form.patchValue({ invoiceDate: '2026-08-10', dueDate: '2026-08-10' });

      expect(component.form.hasError('dueDateBeforeInvoiceDate')).toBe(false);
    });

    it('save() does not call the API when the due date precedes the invoice date', () => {
      setup();
      component.form.patchValue({
        invoiceDate: '2026-08-20',
        dueDate: '2026-08-10',
        customerId: 'cust-1',
        senderId: 'send-1',
      });
      component.lineItems.at(0).patchValue({ description: 'Widget', quantity: 1, unitPrice: 10 });

      component.save();

      httpMock.expectNone(`${environment.apiBaseUrl}/api/invoices`);
    });
  });

  describe('edit mode', () => {
    it('loads the existing invoice and populates line items', () => {
      setup('inv-1');

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/invoices/inv-1`);
      req.flush({
        id: 'inv-1',
        invoiceNumber: 'INV-2026-0001',
        currency: 'USD',
        invoiceDate: '2026-08-01',
        dueDate: '2026-08-15',
        customerId: 'cust-1',
        customerName: 'Acme Corp',
        senderId: 'send-1',
        senderName: 'My Company LLC',
        taxRatePercent: 10,
        notes: null,
        subtotalAmount: 100,
        taxAmount: 10,
        totalAmount: 110,
        lineItems: [{ id: 'li-1', description: 'Widget', quantity: 2, unitPrice: 50, lineTotal: 100 }],
        createdAtUtc: '',
        updatedAtUtc: '',
      });

      expect(component.isEditMode()).toBe(true);
      expect(component.lineItems.length).toBe(1);
      expect(component.lineItems.at(0).value.description).toBe('Widget');
      expect(component.subtotal()).toBe(100);
    });
  });
});

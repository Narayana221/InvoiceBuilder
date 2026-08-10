import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { InvoiceService } from './invoice.service';
import { environment } from '../../../environments/environment';

describe('InvoiceService', () => {
  let service: InvoiceService;
  let httpMock: HttpTestingController;

  const baseUrl = `${environment.apiBaseUrl}/api/invoices`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(InvoiceService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('list() populates invoices and totalCount on success', () => {
    service.list(1, 20);

    const req = httpMock.expectOne(
      (r) => r.url === baseUrl && r.params.get('page') === '1' && r.params.get('pageSize') === '20',
    );
    req.flush({
      items: [
        {
          id: '1',
          invoiceNumber: 'INV-2026-0001',
          customerName: 'Acme Corp',
          senderName: 'My Company LLC',
          invoiceDate: '2026-08-01',
          dueDate: '2026-08-15',
          currency: 'USD',
          totalAmount: 150,
        },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1,
    });

    expect(service.invoices().length).toBe(1);
    expect(service.invoices()[0].invoiceNumber).toBe('INV-2026-0001');
    expect(service.totalCount()).toBe(1);
  });

  it('downloadPdf() requests a Blob with the correct URL and response type', () => {
    const pdfBlob = new Blob(['%PDF-1.4 fake content'], { type: 'application/pdf' });

    service.downloadPdf('abc-123').subscribe((blob) => {
      expect(blob).toBe(pdfBlob);
    });

    const req = httpMock.expectOne(`${baseUrl}/abc-123/pdf`);
    expect(req.request.method).toBe('GET');
    expect(req.request.responseType).toBe('blob');
    req.flush(pdfBlob);
  });

  it('create() posts the request body to the invoices endpoint', () => {
    const request = {
      invoiceDate: '2026-08-01',
      dueDate: '2026-08-15',
      customerId: 'cust-1',
      senderId: 'send-1',
      currency: 'USD',
      taxRatePercent: 20,
      notes: null,
      lineItems: [{ description: 'Widget', quantity: 1, unitPrice: 10 }],
    };

    service.create(request).subscribe();

    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({});
  });
});

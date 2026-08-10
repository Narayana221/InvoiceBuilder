import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CustomerService } from './customer.service';
import { environment } from '../../../environments/environment';

describe('CustomerService', () => {
  let service: CustomerService;
  let httpMock: HttpTestingController;

  const baseUrl = `${environment.apiBaseUrl}/api/customers`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CustomerService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('list() sets loading immediately, then populates customers/totalCount on success', () => {
    service.list(1, 20);
    expect(service.loading()).toBe(true);

    const req = httpMock.expectOne(
      (r) => r.url === baseUrl && r.params.get('page') === '1' && r.params.get('pageSize') === '20',
    );
    expect(req.request.method).toBe('GET');

    req.flush({
      items: [
        {
          id: '1',
          name: 'Acme Corp',
          contactName: null,
          addressLine: '1 Main St',
          city: 'City',
          postalCode: null,
          country: 'USA',
          email: null,
          taxId: null,
          createdAtUtc: '',
          updatedAtUtc: '',
        },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1,
    });

    expect(service.loading()).toBe(false);
    expect(service.customers().length).toBe(1);
    expect(service.customers()[0].name).toBe('Acme Corp');
    expect(service.totalCount()).toBe(1);
    expect(service.error()).toBeNull();
  });

  it('list() sets an error message and clears loading on failure, without touching customers', () => {
    service.list();

    const req = httpMock.expectOne((r) => r.url === baseUrl);
    req.flush('boom', { status: 500, statusText: 'Internal Server Error' });

    expect(service.loading()).toBe(false);
    expect(service.error()).toBe('Failed to load customers.');
    expect(service.customers()).toEqual([]);
  });

  it('create() posts the request body to the customers endpoint', () => {
    const request = {
      name: 'New Co',
      contactName: null,
      addressLine: '1 St',
      city: 'City',
      postalCode: null,
      country: 'USA',
      email: null,
      taxId: null,
    };

    service.create(request).subscribe();

    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({ id: '1', ...request, createdAtUtc: '', updatedAtUtc: '' });
  });

  it('delete() sends a DELETE request to the customer-specific URL', () => {
    service.delete('abc-123').subscribe();

    const req = httpMock.expectOne(`${baseUrl}/abc-123`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});

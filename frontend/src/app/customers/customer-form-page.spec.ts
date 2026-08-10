import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { vi } from 'vitest';
import { CustomerFormPage } from './customer-form-page';
import { environment } from '../../environments/environment';

describe('CustomerFormPage', () => {
  let fixture: ComponentFixture<CustomerFormPage>;
  let component: CustomerFormPage;
  let httpMock: HttpTestingController;
  let router: Router;

  const baseUrl = `${environment.apiBaseUrl}/api/customers`;

  function setup(routeId: string | null = null) {
    TestBed.configureTestingModule({
      imports: [CustomerFormPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap(routeId ? { id: routeId } : {}) } },
        },
      ],
    });
    router = TestBed.inject(Router);
    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(CustomerFormPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  afterEach(() => {
    httpMock.verify();
  });

  it('starts in create mode when there is no route id', () => {
    setup(null);
    expect(component.isEditMode()).toBe(false);
  });

  it('blocks save and marks fields touched when required fields are empty', () => {
    setup(null);

    component.save();

    expect(component.form.invalid).toBe(true);
    expect(component.form.controls.name.touched).toBe(true);
    httpMock.expectNone(baseUrl);
  });

  it('rejects an invalid email format', () => {
    setup(null);
    component.form.controls.email.setValue('not-an-email');
    expect(component.form.controls.email.invalid).toBe(true);
  });

  it('accepts a blank email since it is optional', () => {
    setup(null);
    component.form.controls.email.setValue('');
    expect(component.form.controls.email.valid).toBe(true);
  });

  it('save() with a valid form posts to the API and navigates to the list on success', () => {
    setup(null);
    component.form.setValue({
      name: 'Acme Corp',
      contactName: '',
      addressLine: '123 Main St',
      city: 'Springfield',
      postalCode: '',
      country: 'USA',
      email: '',
      taxId: '',
    });

    const navigateSpy = vi.spyOn(router, 'navigate');
    component.save();

    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.name).toBe('Acme Corp');
    expect(req.request.body.contactName).toBeNull(); // blank string normalized to null before sending

    req.flush({ id: '1', name: 'Acme Corp', createdAtUtc: '', updatedAtUtc: '' });

    expect(navigateSpy).toHaveBeenCalledWith(['/customers']);
  });

  it('edit mode fetches the existing customer and pre-fills the form', () => {
    setup('cust-1');

    const req = httpMock.expectOne(`${baseUrl}/cust-1`);
    req.flush({
      id: 'cust-1',
      name: 'Existing Co',
      contactName: null,
      addressLine: '9 Old St',
      city: 'Oldtown',
      postalCode: null,
      country: 'USA',
      email: null,
      taxId: null,
      createdAtUtc: '',
      updatedAtUtc: '',
    });

    expect(component.isEditMode()).toBe(true);
    expect(component.form.controls.name.value).toBe('Existing Co');
  });
});

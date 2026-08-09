export interface Customer {
  id: string;
  name: string;
  contactName: string | null;
  addressLine: string;
  city: string;
  postalCode: string | null;
  country: string;
  email: string | null;
  taxId: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CustomerRequest {
  name: string;
  contactName: string | null;
  addressLine: string;
  city: string;
  postalCode: string | null;
  country: string;
  email: string | null;
  taxId: string | null;
}

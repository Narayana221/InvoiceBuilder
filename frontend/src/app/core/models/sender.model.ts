export interface Sender {
  id: string;
  name: string;
  contactName: string | null;
  addressLine: string;
  city: string;
  postalCode: string | null;
  country: string;
  email: string | null;
  taxId: string | null;
  bankDetails: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface SenderRequest {
  name: string;
  contactName: string | null;
  addressLine: string;
  city: string;
  postalCode: string | null;
  country: string;
  email: string | null;
  taxId: string | null;
  bankDetails: string | null;
}

export interface InvoiceLineItem {
  id: string;
  description: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface InvoiceLineItemRequest {
  description: string;
  quantity: number;
  unitPrice: number;
}

export interface InvoiceSummary {
  id: string;
  invoiceNumber: string;
  customerName: string;
  senderName: string;
  invoiceDate: string;
  dueDate: string;
  currency: string;
  totalAmount: number;
}

export interface Invoice {
  id: string;
  invoiceNumber: string;
  currency: string;
  invoiceDate: string;
  dueDate: string;
  customerId: string;
  customerName: string;
  senderId: string;
  senderName: string;
  taxRatePercent: number;
  notes: string | null;
  subtotalAmount: number;
  taxAmount: number;
  totalAmount: number;
  lineItems: InvoiceLineItem[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface InvoiceRequest {
  invoiceDate: string;
  dueDate: string;
  customerId: string;
  senderId: string;
  currency: string;
  taxRatePercent: number;
  notes: string | null;
  lineItems: InvoiceLineItemRequest[];
}

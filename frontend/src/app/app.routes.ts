import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'invoices', pathMatch: 'full' },
  {
    path: 'invoices',
    loadComponent: () => import('./invoices/invoices-page').then((m) => m.InvoicesPage),
  },
  {
    path: 'invoices/new',
    loadComponent: () => import('./invoices/invoice-form-page').then((m) => m.InvoiceFormPage),
  },
  {
    path: 'invoices/:id/edit',
    loadComponent: () => import('./invoices/invoice-form-page').then((m) => m.InvoiceFormPage),
  },
  {
    path: 'customers',
    loadComponent: () => import('./customers/customers-page').then((m) => m.CustomersPage),
  },
  {
    path: 'customers/new',
    loadComponent: () => import('./customers/customer-form-page').then((m) => m.CustomerFormPage),
  },
  {
    path: 'customers/:id/edit',
    loadComponent: () => import('./customers/customer-form-page').then((m) => m.CustomerFormPage),
  },
  {
    path: 'senders',
    loadComponent: () => import('./senders/senders-page').then((m) => m.SendersPage),
  },
  {
    path: 'senders/new',
    loadComponent: () => import('./senders/sender-form-page').then((m) => m.SenderFormPage),
  },
  {
    path: 'senders/:id/edit',
    loadComponent: () => import('./senders/sender-form-page').then((m) => m.SenderFormPage),
  },
];

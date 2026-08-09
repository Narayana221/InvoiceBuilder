import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'invoices', pathMatch: 'full' },
  {
    path: 'invoices',
    loadComponent: () => import('./invoices/invoices-page').then((m) => m.InvoicesPage),
  },
  {
    path: 'customers',
    loadComponent: () => import('./customers/customers-page').then((m) => m.CustomersPage),
  },
  {
    path: 'senders',
    loadComponent: () => import('./senders/senders-page').then((m) => m.SendersPage),
  },
];

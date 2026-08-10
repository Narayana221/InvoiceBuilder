import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SenderService } from '../core/services/sender.service';
import { ConfirmDialog } from '../shared/confirm-dialog';
import { Pager } from '../shared/pager';

const PAGE_SIZE = 20;

@Component({
  selector: 'app-senders-page',
  imports: [RouterLink, ConfirmDialog, Pager],
  templateUrl: './senders-page.html',
})
export class SendersPage implements OnInit {
  private readonly senderService = inject(SenderService);

  readonly senders = this.senderService.senders;
  readonly loading = this.senderService.loading;
  readonly error = this.senderService.error;
  readonly totalCount = this.senderService.totalCount;
  readonly pageSize = PAGE_SIZE;

  readonly page = signal(1);
  readonly pendingDeleteId = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.senderService.list(this.page(), this.pageSize);
  }

  onPageChange(page: number): void {
    this.page.set(page);
    this.load();
  }

  confirmDelete(id: string): void {
    this.pendingDeleteId.set(id);
  }

  cancelDelete(): void {
    this.pendingDeleteId.set(null);
  }

  deleteConfirmed(): void {
    const id = this.pendingDeleteId();
    if (!id) return;

    this.senderService.delete(id).subscribe({
      next: () => {
        this.pendingDeleteId.set(null);
        this.load();
      },
      error: () => this.pendingDeleteId.set(null),
    });
  }
}

import { Component, OnInit, inject } from '@angular/core';
import { SenderService } from '../core/services/sender.service';

@Component({
  selector: 'app-senders-page',
  imports: [],
  templateUrl: './senders-page.html',
})
export class SendersPage implements OnInit {
  private readonly senderService = inject(SenderService);

  readonly senders = this.senderService.senders;
  readonly loading = this.senderService.loading;
  readonly error = this.senderService.error;
  readonly totalCount = this.senderService.totalCount;

  ngOnInit(): void {
    this.senderService.list();
  }
}

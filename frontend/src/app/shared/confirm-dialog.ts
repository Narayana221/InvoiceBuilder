import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-confirm-dialog',
  imports: [],
  templateUrl: './confirm-dialog.html',
})
export class ConfirmDialog {
  readonly message = input.required<string>();
  readonly confirmed = output<void>();
  readonly cancelled = output<void>();
}

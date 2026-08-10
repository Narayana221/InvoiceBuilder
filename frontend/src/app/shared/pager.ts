import { Component, computed, input, output } from '@angular/core';

@Component({
  selector: 'app-pager',
  imports: [],
  templateUrl: './pager.html',
})
export class Pager {
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly pageChange = output<number>();

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));

  previous(): void {
    if (this.page() > 1) this.pageChange.emit(this.page() - 1);
  }

  next(): void {
    if (this.page() < this.totalPages()) this.pageChange.emit(this.page() + 1);
  }
}

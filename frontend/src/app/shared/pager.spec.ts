import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Pager } from './pager';

describe('Pager', () => {
  let fixture: ComponentFixture<Pager>;

  function createWith(page: number, pageSize: number, totalCount: number) {
    fixture = TestBed.createComponent(Pager);
    fixture.componentRef.setInput('page', page);
    fixture.componentRef.setInput('pageSize', pageSize);
    fixture.componentRef.setInput('totalCount', totalCount);
    fixture.detectChanges();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [Pager] });
  });

  it('computes totalPages by ceiling division', () => {
    createWith(1, 20, 45);
    expect(fixture.componentInstance.totalPages()).toBe(3);
  });

  it('totalPages is at least 1 even when totalCount is 0', () => {
    createWith(1, 20, 0);
    expect(fixture.componentInstance.totalPages()).toBe(1);
  });

  it('previous() emits page - 1 when not on the first page', () => {
    createWith(2, 20, 45);
    const emitted: number[] = [];
    fixture.componentInstance.pageChange.subscribe((p) => emitted.push(p));

    fixture.componentInstance.previous();

    expect(emitted).toEqual([1]);
  });

  it('previous() does not emit on the first page', () => {
    createWith(1, 20, 45);
    const emitted: number[] = [];
    fixture.componentInstance.pageChange.subscribe((p) => emitted.push(p));

    fixture.componentInstance.previous();

    expect(emitted).toEqual([]);
  });

  it('next() emits page + 1 when not on the last page', () => {
    createWith(1, 20, 45);
    const emitted: number[] = [];
    fixture.componentInstance.pageChange.subscribe((p) => emitted.push(p));

    fixture.componentInstance.next();

    expect(emitted).toEqual([2]);
  });

  it('next() does not emit on the last page', () => {
    createWith(3, 20, 45);
    const emitted: number[] = [];
    fixture.componentInstance.pageChange.subscribe((p) => emitted.push(p));

    fixture.componentInstance.next();

    expect(emitted).toEqual([]);
  });

  it('disables both buttons when there is only one page', () => {
    createWith(1, 20, 20);
    const buttons = fixture.nativeElement.querySelectorAll('button');
    expect((buttons[0] as HTMLButtonElement).disabled).toBe(true);
    expect((buttons[1] as HTMLButtonElement).disabled).toBe(true);
  });
});

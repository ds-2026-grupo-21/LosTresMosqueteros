import { ListService, LocalizationPipe, ShortDatePipe } from '@abp/ng.core';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, catchError } from 'rxjs';
import { PantryItemDto, PantryItemStatus, PantryService } from '@proxy/pantry';

type ListState = 'loading' | 'error' | 'success';

@Component({
  selector: 'app-pantry',
  imports: [LocalizationPipe, ShortDatePipe],
  templateUrl: './pantry.component.html',
  styleUrl: './pantry.component.scss',
  providers: [ListService],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PantryComponent {
  readonly list = inject(ListService);
  private readonly pantry = inject(PantryService);
  private readonly destroyRef = inject(DestroyRef);

  readonly state = signal<ListState>('loading');
  readonly items = signal<PantryItemDto[]>([]);
  readonly totalCount = signal(0);
  readonly consumed = PantryItemStatus.Consumed;

  constructor() {
    this.list.hookToQuery(query => {
      this.state.set('loading');
      return this.pantry.getList(query, { skipHandleError: true }).pipe(
        catchError(() => {
          this.state.set('error');
          return EMPTY;
        }),
      );
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(result => {
      this.items.set(result.items ?? []);
      this.totalCount.set(result.totalCount ?? 0);
      this.state.set('success');
    });
  }

  retry(): void {
    this.list.getWithoutPageReset();
  }

  previous(): void {
    if (this.list.page > 0) this.list.page--;
  }

  next(): void {
    if ((this.list.page + 1) * this.list.maxResultCount < this.totalCount()) this.list.page++;
  }
}

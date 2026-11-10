import { ListService, LocalizationPipe, ShortDatePipe } from '@abp/ng.core';
import { Confirmation, ConfirmationService } from '@abp/ng.theme.shared';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Observable, catchError, finalize } from 'rxjs';
import { PantryItemDto, PantryItemStatus, PantryService } from '@proxy/pantry';
import { PantryEditModalComponent } from './pantry-edit-modal/pantry-edit-modal.component';

type ListState = 'loading' | 'error' | 'success';

@Component({
  selector: 'app-pantry',
  imports: [LocalizationPipe, ShortDatePipe, PantryEditModalComponent],
  templateUrl: './pantry.component.html',
  styleUrl: './pantry.component.scss',
  providers: [ListService],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PantryComponent {
  readonly list = inject(ListService);
  private readonly pantry = inject(PantryService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly destroyRef = inject(DestroyRef);

  readonly state = signal<ListState>('loading');
  readonly items = signal<PantryItemDto[]>([]);
  readonly totalCount = signal(0);
  readonly consumed = PantryItemStatus.Consumed;

  readonly editingItem = signal<PantryItemDto | null>(null);
  readonly actionBusyId = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);

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
      const total = result.totalCount ?? 0;
      const lastPage = Math.max(0, Math.ceil(total / this.list.maxResultCount) - 1);
      if ((result.items?.length ?? 0) === 0 && this.list.page > lastPage) {
        this.list.page = lastPage;
        return;
      }
      this.items.set(result.items ?? []);
      this.totalCount.set(total);
      this.state.set('success');
    });
  }

  retry(): void {
    this.list.getWithoutPageReset();
  }

  edit(item: PantryItemDto): void {
    if (this.actionBusyId()) return;
    this.editingItem.set(item);
  }

  closeEdit(): void {
    this.editingItem.set(null);
  }

  onEditSaved(): void {
    this.editingItem.set(null);
    this.list.getWithoutPageReset();
  }

  markConsumed(item: PantryItemDto): void {
    const id = item.id;
    if (!id || item.status !== PantryItemStatus.Active || this.actionBusyId()) return;

    this.runAction(id, this.pantry.markConsumed(id, { skipHandleError: true }));
  }

  delete(item: PantryItemDto): void {
    const id = item.id;
    if (!id || this.actionBusyId()) return;

    this.actionError.set(null);
    this.actionBusyId.set(id);

    this.confirmation
      .warn('::Pantry:DeleteConfirmation', '::Pantry:DeleteTitle', {
        yesText: '::Pantry:Delete',
        cancelText: '::Pantry:Cancel',
      })
      .subscribe(status => {
        if (status !== Confirmation.Status.confirm) {
          this.actionBusyId.set(null);
          return;
        }

        this.runAction(id, this.pantry.delete(id, { skipHandleError: true }));
      });
  }

  private runAction(id: string, request: Observable<unknown>): void {
    this.actionBusyId.set(id);
    this.actionError.set(null);

    request.pipe(finalize(() => this.actionBusyId.set(null))).subscribe({
      next: () => this.list.getWithoutPageReset(),
      error: () => this.actionError.set('::Pantry:ActionError'),
    });
  }

  previous(): void {
    if (this.list.page > 0) this.list.page--;
  }

  next(): void {
    if ((this.list.page + 1) * this.list.maxResultCount < this.totalCount()) this.list.page++;
  }
}

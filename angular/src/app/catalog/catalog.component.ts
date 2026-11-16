import { LocalizationPipe } from '@abp/ng.core';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable, Subject, catchError, map, of, switchMap, takeUntil } from 'rxjs';
import { ConsultaProductoExternoService, GuardarProductoExternoService, ExternalProductDto, ExternalProductSearchResultDto, ResultadoConsultaExterna } from '@proxy/productos';
import { RouterLink } from '@angular/router';
import { PantryService } from '@proxy/pantry';
import { QUANTITY_MAX, UNIT_MAX_LENGTH, validDate, maxTwoDecimals } from '../pantry/pantry-edit-modal/pantry-edit-modal.component';
import { FoodWarningsComponent } from '../warnings/food-warnings.component';
import { FoodWarningDto } from '@proxy/preferences';

type State = 'idle' | 'loading' | 'results' | 'detail' | 'empty' | 'notFound' | 'rate' | 'error' | 'invalid';
type Request = { kind: 'search'; text: string; page: number } | { kind: 'detail'; code: string };

@Component({
  selector: 'app-catalog',
    imports: [LocalizationPipe, ReactiveFormsModule, FoodWarningsComponent, RouterLink],
  templateUrl: './catalog.component.html',
  styleUrl: './catalog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CatalogComponent {
  private readonly service = inject(ConsultaProductoExternoService);
  private readonly pantry = inject(PantryService);
  private readonly fb = inject(FormBuilder);
  private readonly saveService = inject(GuardarProductoExternoService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly requests = new Subject<Request>();
  private readonly cancel = new Subject<void>();
  readonly text = new FormControl('', { nonNullable: true });
  readonly code = new FormControl('', { nonNullable: true });
  readonly state = signal<State>('idle');
  readonly results = signal<ExternalProductSearchResultDto | null>(null);
  readonly detail = signal<ExternalProductDto | null>(null);
  readonly imageFailed = signal(false);
  readonly addOpen = signal(false);
  readonly adding = signal(false);
  readonly savedProductId = signal<string | null>(null);
  readonly actionWarnings = signal<FoodWarningDto | null>(null);
  readonly actionEvaluated = signal(false);
  readonly addMessage = signal<'saved' | 'saveError' | 'addError' | null>(null);
  readonly addForm = this.fb.group({
    quantity: this.fb.control<number | null>(null, [Validators.min(0), Validators.max(QUANTITY_MAX), maxTwoDecimals]),
    unit: this.fb.control<string | null>(null, [Validators.maxLength(UNIT_MAX_LENGTH)]),
    expirationDate: this.fb.control<string | null>(null, [validDate]),
  });
  readonly saving = signal(false);
  readonly saveMessage = signal<'saved' | 'notFound' | 'rate' | 'error' | null>(null);
  private lastRequest: Request | null = null;
  private searchText = '';

  constructor() {
    this.requests.pipe(switchMap(request => {
      this.lastRequest = request;
      this.state.set('loading');
      const call: Observable<ExternalProductSearchResultDto | ExternalProductDto> = request.kind === 'search'
        ? this.service.search({ texto: request.text, pagina: request.page, tamanoPagina: 10 }, { skipHandleError: true })
        : this.service.get({ codigoBarras: request.code }, { skipHandleError: true });
      return call.pipe(takeUntil(this.cancel), map(value => ({ request, value })), catchError(() => of({ request, value: null })));
    }), takeUntilDestroyed(this.destroyRef)).subscribe(({ request, value }) => {
      if (!value) { this.state.set('error'); return; }
      if (value.resultado === ResultadoConsultaExterna.LimiteDeSolicitudesExcedido) { this.state.set('rate'); return; }
      if (value.resultado === ResultadoConsultaExterna.ServicioNoDisponible) { this.state.set('error'); return; }
      if (request.kind === 'search') {
        const result = value as ExternalProductSearchResultDto;
        this.results.set(result);
        this.state.set(result.productos?.length ? 'results' : 'empty');
      } else {
        if (value.resultado === ResultadoConsultaExterna.NoEncontrado) { this.state.set('notFound'); return; }
        this.detail.set(value as ExternalProductDto);
        this.imageFailed.set(false);
        this.state.set('detail');
      }
    });
  }

  search(): void {
    if (this.saving() || this.adding()) return;
    const text = this.text.value.trim();
    if (!text || text.length > 200) { this.cancel.next(); this.state.set('invalid'); return; }
    this.searchText = text;
    this.requests.next({ kind: 'search', text, page: 1 });
  }
  openCode(): void { this.open(this.code.value.trim()); }
  open(code: string): void {
    if (this.saving() || this.adding()) return;
    this.saveMessage.set(null);
    this.addOpen.set(false);
    this.savedProductId.set(null);
    this.actionWarnings.set(null);
    this.actionEvaluated.set(false);
    this.addMessage.set(null);
    this.addForm.reset();
    if (!/^\d{8,14}$/.test(code)) { this.cancel.next(); this.state.set('invalid'); return; }
    this.requests.next({ kind: 'detail', code });
  }
  page(delta: number): void {
    const page = (this.results()?.pagina ?? 1) + delta;
    if (page < 1 || page * 10 > 10000) return;
    this.requests.next({ kind: 'search', text: this.searchText, page });
  }
  canNext(): boolean {
    const result = this.results();
    const page = result?.pagina ?? 1;
    if (page * 10 >= 10000) return false;
    if (result?.paginasDisponibles != null) return page < result.paginasDisponibles;
    return (result?.productos?.length ?? 0) === 10;
  }
  saveProduct(): void {
    const product = this.detail();
    const code = product?.codigoBarras;
    if (this.saving() || this.adding() || !code || !/^\d{8,14}$/.test(code)) return;
    this.saving.set(true);
    this.saveMessage.set(null);
    this.saveService.save({ codigoBarras: code }, { skipHandleError: true })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: result => {
          this.saving.set(false);
          if (result.resultado === ResultadoConsultaExterna.Encontrado && result.producto?.id) {
            this.savedProductId.set(result.producto.id);
            this.saveMessage.set('saved');
            this.actionWarnings.set(result.advertencias ?? null);
            this.actionEvaluated.set(true);
          } else if (result.resultado === ResultadoConsultaExterna.NoEncontrado) this.saveMessage.set('notFound');
          else if (result.resultado === ResultadoConsultaExterna.LimiteDeSolicitudesExcedido) this.saveMessage.set('rate');
          else this.saveMessage.set('error');
        },
        error: () => { this.saving.set(false); this.saveMessage.set('error'); },
      });
  }
  showAdd(): void {
    if (this.saving() || this.adding()) return;
    this.addMessage.set(null);
    this.addForm.reset();
    this.addOpen.set(true);
  }
  cancelAdd(): void { if (!this.adding()) this.addOpen.set(false); }
  addProduct(): void {
    if (this.saving() || this.adding()) return;
    if (this.addForm.invalid) { this.addForm.markAllAsTouched(); return; }
    const code = this.detail()?.codigoBarras;
    if (!code || !/^\d{8,14}$/.test(code)) { this.addMessage.set('saveError'); return; }
    const values = this.addForm.getRawValue();
    this.addMessage.set(null);
    this.adding.set(true);
    const create = (id: string) => {
      this.pantry.create({ productId: id, quantity: values.quantity, unit: values.unit?.trim() || null, expirationDate: values.expirationDate || null }, { skipHandleError: true })
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: item => {
            this.adding.set(false); this.addMessage.set('saved'); this.addOpen.set(false);
            this.actionWarnings.set(item.advertencias ?? null);
            this.actionEvaluated.set(true);
          },
          error: () => { this.adding.set(false); this.addMessage.set('addError'); },
        });
    };
    const id = this.savedProductId();
    if (id) { create(id); return; }
    this.saveService.save({ codigoBarras: code }, { skipHandleError: true })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: result => {
          if (result.resultado !== ResultadoConsultaExterna.Encontrado || !result.producto?.id) {
            this.adding.set(false); this.addMessage.set('saveError'); return;
          }
          this.savedProductId.set(result.producto.id);
          this.actionWarnings.set(result.advertencias ?? null);
          this.actionEvaluated.set(true);
          create(result.producto.id);
        },
        error: () => { this.adding.set(false); this.addMessage.set('saveError'); },
      });
  }
  back(): void { if (!this.saving() && !this.adding()) this.state.set(this.results()?.productos?.length ? 'results' : 'idle'); }
  retry(): void { if (this.lastRequest) this.requests.next(this.lastRequest); }
}

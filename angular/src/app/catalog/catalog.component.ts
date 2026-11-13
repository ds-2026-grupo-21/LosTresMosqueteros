import { LocalizationPipe } from '@abp/ng.core';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Observable, Subject, catchError, map, of, switchMap, takeUntil } from 'rxjs';
import { ConsultaProductoExternoService, ExternalProductDto, ExternalProductSearchResultDto, ResultadoConsultaExterna } from '@proxy/productos';

type State = 'idle' | 'loading' | 'results' | 'detail' | 'empty' | 'notFound' | 'rate' | 'error' | 'invalid';
type Request = { kind: 'search'; text: string; page: number } | { kind: 'detail'; code: string };

@Component({
  selector: 'app-catalog',
  imports: [LocalizationPipe, ReactiveFormsModule],
  templateUrl: './catalog.component.html',
  styleUrl: './catalog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CatalogComponent {
  private readonly service = inject(ConsultaProductoExternoService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly requests = new Subject<Request>();
  private readonly cancel = new Subject<void>();
  readonly text = new FormControl('', { nonNullable: true });
  readonly code = new FormControl('', { nonNullable: true });
  readonly state = signal<State>('idle');
  readonly results = signal<ExternalProductSearchResultDto | null>(null);
  readonly detail = signal<ExternalProductDto | null>(null);
  readonly imageFailed = signal(false);
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
    const text = this.text.value.trim();
    if (!text || text.length > 200) { this.cancel.next(); this.state.set('invalid'); return; }
    this.searchText = text;
    this.requests.next({ kind: 'search', text, page: 1 });
  }
  openCode(): void { this.open(this.code.value.trim()); }
  open(code: string): void {
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
  back(): void { this.state.set(this.results()?.productos?.length ? 'results' : 'idle'); }
  retry(): void { if (this.lastRequest) this.requests.next(this.lastRequest); }
}

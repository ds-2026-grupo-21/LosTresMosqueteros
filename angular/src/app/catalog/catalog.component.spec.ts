import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Pipe, PipeTransform } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { ConsultaProductoExternoService, ExternalProductDto, ExternalProductSearchResultDto, ResultadoConsultaExterna } from '@proxy/productos';
import { Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { CatalogComponent } from './catalog.component';

@Pipe({ name: 'abpLocalization' })
class TestLocalizationPipe implements PipeTransform { transform(value: string): string { return value; } }

describe('CatalogComponent', () => {
  let fixture: ComponentFixture<CatalogComponent>;
  let searches: Subject<ExternalProductSearchResultDto>[];
  let details: Subject<ExternalProductDto>[];
  let service: { search: ReturnType<typeof vi.fn>; get: ReturnType<typeof vi.fn> };
  beforeEach(() => {
    searches = []; details = [];
    service = {
      search: vi.fn(() => { const value = new Subject<ExternalProductSearchResultDto>(); searches.push(value); return value; }),
      get: vi.fn(() => { const value = new Subject<ExternalProductDto>(); details.push(value); return value; }),
    };
    TestBed.configureTestingModule({ imports: [CatalogComponent], providers: [{ provide: ConsultaProductoExternoService, useValue: service }] });
    TestBed.overrideComponent(CatalogComponent, { set: { imports: [TestLocalizationPipe, ReactiveFormsModule] } });
    fixture = TestBed.createComponent(CatalogComponent); fixture.detectChanges();
  });
  afterEach(() => TestBed.resetTestingModule());

  it('envía los formularios sin navegar ni recargar la página', () => {
    const c = fixture.componentInstance;
    c.text.setValue('leche');
    const searchSubmit = new Event('submit', { bubbles: true, cancelable: true });
    fixture.nativeElement.querySelectorAll('form')[0].dispatchEvent(searchSubmit);
    expect(searchSubmit.defaultPrevented).toBe(true);
    expect(service.search).toHaveBeenCalledTimes(1);
    c.code.setValue('3017620422003');
    const codeSubmit = new Event('submit', { bubbles: true, cancelable: true });
    fixture.nativeElement.querySelectorAll('form')[1].dispatchEvent(codeSubmit);
    expect(codeSubmit.defaultPrevented).toBe(true);
    expect(service.get).toHaveBeenCalledTimes(1);
  });

  it('reinicia la paginación y descarta respuestas anteriores', () => {
    const c = fixture.componentInstance;
    c.text.setValue('leche'); c.search();
    searches[0].next({ pagina: 1, productos: [{ codigoBarras: '00000001', nombre: 'Leche' }], paginasDisponibles: 2 });
    c.page(1); c.text.setValue('arroz'); c.search();
    searches[1].next({ pagina: 2, productos: [{ codigoBarras: '00000002', nombre: 'Viejo' }] });
    searches[2].next({ pagina: 1, productos: [{ codigoBarras: '00000003', nombre: 'Arroz' }] });
    expect(service.search.mock.calls[2][0]).toMatchObject({ texto: 'arroz', pagina: 1 });
    expect(c.results()?.productos?.[0].nombre).toBe('Arroz');
  });

  it('conserva ceros iniciales y valida el código antes de consultar', () => {
    const c = fixture.componentInstance;
    c.code.setValue('00000000'); c.openCode();
    expect(service.get.mock.calls[0][0]).toEqual({ codigoBarras: '00000000' });
    details[0].next({ resultado: ResultadoConsultaExterna.NoEncontrado });
    expect(c.state()).toBe('notFound');
    c.code.setValue('123'); c.openCode();
    expect(c.state()).toBe('invalid'); expect(service.get).toHaveBeenCalledTimes(1);
  });

  it('descarta una respuesta pendiente si la siguiente entrada es inválida', () => {
    const c = fixture.componentInstance; c.text.setValue('leche'); c.search();
    c.code.setValue('1'); c.openCode();
    searches[0].next({ productos: [{ codigoBarras: '00000001', nombre: 'Leche' }] });
    expect(c.state()).toBe('invalid');
  });

  it('distingue vacío, límite y error y permite reintentar', () => {
    const c = fixture.componentInstance; c.text.setValue('arroz'); c.search();
    searches[0].next({ productos: [] }); expect(c.state()).toBe('empty');
    c.search(); searches[1].next({ resultado: ResultadoConsultaExterna.LimiteDeSolicitudesExcedido });
    expect(c.state()).toBe('rate'); c.retry(); searches[2].error(new Error());
    expect(c.state()).toBe('error'); c.retry(); expect(c.state()).toBe('loading');
  });

  it('muestra datos ausentes y vuelve a los resultados', () => {
    const c = fixture.componentInstance; c.text.setValue('leche'); c.search();
    searches[0].next({ productos: [{ codigoBarras: '00000001', nombre: 'Leche' }] });
    c.open('00000001'); details[0].next({ resultado: ResultadoConsultaExterna.Encontrado, nombre: 'Leche' });
    fixture.detectChanges(); expect(fixture.nativeElement.textContent).toContain('::Catalog:Unavailable');
    c.back(); expect(c.state()).toBe('results');
  });
});

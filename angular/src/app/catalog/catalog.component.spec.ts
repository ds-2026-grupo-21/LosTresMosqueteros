import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Pipe, PipeTransform } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { provideRouter, RouterLink } from '@angular/router';
import { PantryService } from '@proxy/pantry';
import { FoodField } from '@proxy/preferences';
import { ConsultaProductoExternoService, GuardarProductoExternoService, ExternalProductDto, ExternalProductSearchResultDto, ResultadoConsultaExterna } from '@proxy/productos';
import { Subject, of, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { FoodWarningsComponent } from '../warnings/food-warnings.component';
import { CatalogComponent } from './catalog.component';

@Pipe({ name: 'abpLocalization' })
class TestLocalizationPipe implements PipeTransform { transform(value: string): string { return value; } }

describe('CatalogComponent', () => {
  let fixture: ComponentFixture<CatalogComponent>;
  let searches: Subject<ExternalProductSearchResultDto>[];
  let details: Subject<ExternalProductDto>[];
  let service: { search: ReturnType<typeof vi.fn>; get: ReturnType<typeof vi.fn> };
  let saveService: { save: ReturnType<typeof vi.fn> };
  let pantry: { create: ReturnType<typeof vi.fn> };
  beforeEach(() => {
    searches = []; details = [];
    pantry = { create: vi.fn() };
    saveService = { save: vi.fn() };
    service = {
      search: vi.fn(() => { const value = new Subject<ExternalProductSearchResultDto>(); searches.push(value); return value; }),
      get: vi.fn(() => { const value = new Subject<ExternalProductDto>(); details.push(value); return value; }),
    };
    TestBed.configureTestingModule({ imports: [CatalogComponent], providers: [provideRouter([]), { provide: PantryService, useValue: pantry }, { provide: ConsultaProductoExternoService, useValue: service }, { provide: GuardarProductoExternoService, useValue: saveService }] });
    TestBed.overrideComponent(CatalogComponent, { set: { imports: [TestLocalizationPipe, ReactiveFormsModule, FoodWarningsComponent, RouterLink] } });
    TestBed.overrideComponent(FoodWarningsComponent, { set: { imports: [TestLocalizationPipe] } });
    fixture = TestBed.createComponent(CatalogComponent); fixture.detectChanges();
  });
  afterEach(() => TestBed.resetTestingModule());

  it('guarda el código del detalle y muestra el resultado sin crear un ítem', () => {
    const c = fixture.componentInstance;
    c.open('00000001'); details[0].next({ resultado: ResultadoConsultaExterna.Encontrado, codigoBarras: '00000001' });
    const pending = new Subject<any>(); saveService.save.mockReturnValue(pending);
    c.saveProduct(); c.saveProduct();
    expect(saveService.save).toHaveBeenCalledTimes(1);
    expect(saveService.save.mock.calls[0][0]).toEqual({ codigoBarras: '00000001' });
    pending.next({ resultado: ResultadoConsultaExterna.Encontrado, producto: { id: '123' } });
    expect(c.saveMessage()).toBe('saved'); expect(c.saving()).toBe(false);
  });

  it('permite reintentar cuando falla el guardado', () => {
    const c = fixture.componentInstance;
    c.open('00000001'); details[0].next({ resultado: ResultadoConsultaExterna.Encontrado, codigoBarras: '00000001' });
    saveService.save.mockReturnValueOnce(throwError(() => new Error())).mockReturnValueOnce(of({ resultado: ResultadoConsultaExterna.NoEncontrado }));
    c.saveProduct(); expect(c.saveMessage()).toBe('error');
    c.saveProduct(); expect(c.saveMessage()).toBe('notFound');
  });

  it('mantiene separadas las advertencias externas y las del producto guardado', () => {
    const c = fixture.componentInstance;
    const external = { hasPreferences: true, showAlerts: true, matches: [{ preference: 'Leche', field: FoodField.Ingredients }], unverifiable: [] };
    const stored = { hasPreferences: true, showAlerts: true, matches: [], unverifiable: [] };
    c.open('00000001');
    details[0].next({ resultado: ResultadoConsultaExterna.Encontrado, codigoBarras: '00000001', ingredientes: 'Leche', advertencias: external });
    saveService.save.mockReturnValue(of({ resultado: ResultadoConsultaExterna.Encontrado, producto: { id: '123' }, advertencias: stored }));
    c.saveProduct(); fixture.detectChanges();
    expect(c.detail()?.advertencias).toEqual(external);
    expect(c.actionWarnings()).toEqual(stored);
    expect(fixture.nativeElement.textContent).toContain('::Catalog:StoredWarningsExplanation');
    expect(fixture.nativeElement.textContent).toContain('::Warnings:MatchTitle');
    expect(fixture.nativeElement.textContent).toContain('::Warnings:NoMatches');
  });

  it('avisa si la respuesta de guardado carece de evaluación', () => {
    const c = fixture.componentInstance;
    c.open('00000001'); details[0].next({ resultado: ResultadoConsultaExterna.Encontrado, codigoBarras: '00000001' });
    saveService.save.mockReturnValue(of({ resultado: ResultadoConsultaExterna.Encontrado, producto: { id: '123' } }));
    c.saveProduct(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('::Warnings:EvaluationError');
  });

  it('guarda primero y agrega una sola vez con los datos opcionales', () => {
    const c = fixture.componentInstance;
    c.open('00000001'); details[0].next({ resultado: ResultadoConsultaExterna.Encontrado, codigoBarras: '00000001' });
    const saved = new Subject<any>(); const added = new Subject<any>();
    saveService.save.mockReturnValue(saved); pantry.create.mockReturnValue(added);
    c.showAdd(); c.addForm.patchValue({ quantity: 2.5, unit: ' kg ', expirationDate: '2026-12-01' });
    c.addProduct(); c.addProduct(); expect(saveService.save).toHaveBeenCalledTimes(1); expect(pantry.create).not.toHaveBeenCalled();
    saved.next({ resultado: ResultadoConsultaExterna.Encontrado, producto: { id: 'id-1' } });
    expect(pantry.create).toHaveBeenCalledWith({ productId: 'id-1', quantity: 2.5, unit: 'kg', expirationDate: '2026-12-01' }, expect.anything());
    added.next({ id: 'item-1' }); expect(c.addMessage()).toBe('saved');
  });
  it('conserva el formulario y reutiliza el producto si falla el agregado', () => {
    const c = fixture.componentInstance;
    c.open('00000001'); details[0].next({ resultado: ResultadoConsultaExterna.Encontrado, codigoBarras: '00000001' });
    saveService.save.mockReturnValue(of({ resultado: ResultadoConsultaExterna.Encontrado, producto: { id: 'id-1' } }));
    pantry.create.mockReturnValueOnce(throwError(() => new Error())).mockReturnValueOnce(of({ id: 'item-1' }));
    c.showAdd(); c.addForm.patchValue({ quantity: 3 }); c.addProduct();
    expect(c.addMessage()).toBe('addError'); expect(c.addForm.value.quantity).toBe(3);
    c.addProduct(); expect(saveService.save).toHaveBeenCalledTimes(1); expect(c.addMessage()).toBe('saved');
  });
  it('rechaza valores inválidos y cancelar no crea ítems', () => {
    const c = fixture.componentInstance;
    c.open('00000001'); details[0].next({ resultado: ResultadoConsultaExterna.Encontrado, codigoBarras: '00000001' });
    c.showAdd(); c.addForm.patchValue({ quantity: -1 }); c.addProduct();
    expect(saveService.save).not.toHaveBeenCalled(); expect(pantry.create).not.toHaveBeenCalled();
    c.cancelAdd(); expect(c.addOpen()).toBe(false);
  });
  it('no crea un ítem si falla el guardado del producto', () => {
    const c = fixture.componentInstance;
    c.open('00000001'); details[0].next({ resultado: ResultadoConsultaExterna.Encontrado, codigoBarras: '00000001' });
    saveService.save.mockReturnValue(of({ resultado: ResultadoConsultaExterna.ServicioNoDisponible }));
    c.showAdd(); c.addProduct(); expect(c.addMessage()).toBe('saveError'); expect(pantry.create).not.toHaveBeenCalled();
  });

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

  it('el detalle muestra las coincidencias y los datos no verificables de la evaluación', () => {
    const c = fixture.componentInstance;
    c.open('00000001');
    details[0].next({
      resultado: ResultadoConsultaExterna.Encontrado,
      nombre: 'Galletas',
      ingredientes: 'Harina, leche',
      advertencias: {
        hasPreferences: true,
        showAlerts: true,
        matches: [{ preference: 'Leche', field: FoodField.Ingredients }],
        unverifiable: [FoodField.Allergens],
      },
    });
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('::Warnings:MatchTitle');
    expect(text).toContain('Leche');
    expect(text).toContain('::Warnings:Unverifiable');
    expect(text).toContain('::Warnings:Field:Allergens');
    expect(service.get).toHaveBeenCalledTimes(1);
  });

  it('con alertas apagadas oculta las coincidencias pero la pantalla conserva fuente y aclaración, sin duplicarlas', () => {
    const c = fixture.componentInstance;
    c.open('00000001');
    details[0].next({
      resultado: ResultadoConsultaExterna.Encontrado,
      nombre: 'Galletas',
      advertencias: {
        hasPreferences: true,
        showAlerts: false,
        matches: [{ preference: 'Leche', field: FoodField.Ingredients }],
        unverifiable: [FoodField.Allergens],
      },
    });
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;
    expect(text).not.toContain('::Warnings:MatchTitle');
    expect(text).toContain('::Warnings:Unverifiable');
    expect(text.split('::Catalog:Source').length - 1).toBe(1);
    expect(text.split('::Catalog:Disclaimer').length - 1).toBe(1);
  });

  it('un detalle sin evaluación no muestra avisos personalizados', () => {
    const c = fixture.componentInstance;
    c.open('00000001');
    details[0].next({ resultado: ResultadoConsultaExterna.Encontrado, nombre: 'Galletas' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('::Warnings:');
  });
});

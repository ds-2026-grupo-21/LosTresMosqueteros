import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Pipe, PipeTransform } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { provideRouter, RouterLink } from '@angular/router';
import { PreferenceService, UserPreferenceDto } from '@proxy/preferences';
import { Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { PreferencesComponent } from './preferences.component';

@Pipe({ name: 'abpLocalization' })
class TestLocalizationPipe implements PipeTransform { transform(value: string): string { return value; } }

describe('PreferencesComponent', () => {
  let fixture: ComponentFixture<PreferencesComponent>;
  let loaded: Subject<UserPreferenceDto>;
  let saved: Subject<UserPreferenceDto>;
  let get: ReturnType<typeof vi.fn>;
  let update: ReturnType<typeof vi.fn>;
  beforeEach(() => {
    loaded = new Subject(); saved = new Subject();
    get = vi.fn(() => loaded.asObservable()); update = vi.fn(() => saved.asObservable());
    TestBed.configureTestingModule({ imports: [PreferencesComponent], providers: [
      provideRouter([]), { provide: PreferenceService, useValue: { get, update } },
    ] });
    TestBed.overrideComponent(PreferencesComponent, { set: {
      imports: [TestLocalizationPipe, ReactiveFormsModule, RouterLink],
    } });
    fixture = TestBed.createComponent(PreferencesComponent); fixture.detectChanges();
  });
  afterEach(() => TestBed.resetTestingModule());

  it('distingue carga y vacío', () => {
    expect(fixture.nativeElement.textContent).toContain('::Preferences:Loading');
    expect(fixture.nativeElement.textContent).not.toContain('::Preferences:Empty');
    loaded.next({ showAlerts: true, watchedItems: [] }); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('::Preferences:Empty');
  });
  it('permite reintentar una carga fallida', () => {
    loaded.error(new Error()); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('::Preferences:LoadError');
    loaded = new Subject(); fixture.componentInstance.load();
    loaded.next({ watchedItems: ['Gluten'], showAlerts: false }); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Gluten');
    expect(fixture.componentInstance.form.controls.showAlerts.value).toBe(false);
  });
  it('agrega y quita sin admitir repetidos ni entradas vacías o largas', () => {
    loaded.next({ watchedItems: [] }); const c = fixture.componentInstance;
    for (const text of ['  Maní  ', 'MANI', ' ', 'x'.repeat(101)]) { c.entry.setValue(text); c.add(); }
    expect(c.items()).toEqual(['Maní']); c.remove(0); expect(c.items()).toEqual([]);
  });
  it('rechaza la entrada 51', () => {
    loaded.next({ watchedItems: Array.from({ length: 50 }, (_, i) => `item ${i}`) });
    const c = fixture.componentInstance; c.entry.setValue('otra'); c.add();
    expect(c.items()).toHaveLength(50);
  });
  it('no pierde una entrada sin agregar', () => {
    loaded.next({ watchedItems: [] }); const c = fixture.componentInstance;
    c.entry.setValue('Gluten'); c.save(); expect(update).not.toHaveBeenCalled();
    expect(c.message()).toBe('::Preferences:PendingEntry');
  });
  it('conserva los cambios ante error y evita envíos simultáneos', () => {
    loaded.next({ watchedItems: ['Gluten'] }); const c = fixture.componentInstance;
    c.form.controls.showAlerts.setValue(false); c.save(); c.save();
    expect(update).toHaveBeenCalledTimes(1); saved.error(new Error());
    expect(c.items()).toEqual(['Gluten']); expect(c.form.controls.showAlerts.value).toBe(false);
    expect(c.busy()).toBe(false); expect(c.message()).toBe('::Preferences:SaveError');
  });
  it('muestra la respuesta normalizada del servidor y permite vaciar', () => {
    loaded.next({ watchedItems: ['Gluten'] }); const c = fixture.componentInstance;
    c.remove(0); c.save(); expect(update).toHaveBeenCalledWith(
      { watchedItems: [], showAlerts: true, photoDataUrl: null }, { skipHandleError: true });
    saved.next({ watchedItems: [], showAlerts: true }); saved.complete();
    expect(c.message()).toBe('::Preferences:Saved'); expect(c.busy()).toBe(false);
  });
  it('la foto es opcional y rechaza archivos grandes o de otro tipo sin perder preferencias', async () => {
    loaded.next({ watchedItems: ['Gluten'] }); const c = fixture.componentInstance;
    for (const file of [new File(['<svg/>'], 'foto.svg', { type: 'image/svg+xml' }),
      new File([new Uint8Array(80001)], 'foto.png', { type: 'image/png' })]) {
      await c.choosePhoto({ target: { files: [file], value: 'foto' } } as unknown as Event);
      expect(c.message()).toBe('::Preferences:PhotoError'); expect(c.photo()).toBeNull();
      expect(c.items()).toEqual(['Gluten']); expect(c.busy()).toBe(false);
    }
  });
  it('lee una foto local y la incluye en el guardado', async () => {
    loaded.next({ watchedItems: [] }); const c = fixture.componentInstance;
    const file = new File([new Uint8Array([137,80,78,71,13,10,26,10,0,0,0,0])], 'foto.png', { type: 'image/png' });
    await c.choosePhoto({ target: { files: [file], value: 'foto' } } as unknown as Event);
    expect(c.photo()).toMatch(/^data:image\/png;base64,/); c.save();
    expect(update).toHaveBeenCalledWith(expect.objectContaining({ photoDataUrl: c.photo() }), { skipHandleError: true });
  });
});

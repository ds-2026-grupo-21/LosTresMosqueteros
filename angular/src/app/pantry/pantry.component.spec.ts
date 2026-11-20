import { LIST_QUERY_DEBOUNCE_TIME, PagedResultDto } from '@abp/ng.core';
import { Pipe, PipeTransform } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ExpirationState, PantryItemDto, PantryItemStatus, PantryService } from '@proxy/pantry';
import { PreferenceService, UserPreferenceDto } from '@proxy/preferences';
import { Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { PantryComponent } from './pantry.component';

@Pipe({ name: 'abpLocalization' })
class TestLocalizationPipe implements PipeTransform {
  transform(value: string): string { return value; }
}

@Pipe({ name: 'shortDate' })
class TestShortDatePipe implements PipeTransform {
  transform(value: string): string { return value; }
}

const datedItem: PantryItemDto = { id: '1', status: PantryItemStatus.Active, expirationDate: '2026-11-21' };

describe('PantryComponent', () => {
  let fixture: ComponentFixture<PantryComponent>;
  let response: Subject<PagedResultDto<PantryItemDto>>;
  let getList: ReturnType<typeof vi.fn>;
  let preferences: Subject<UserPreferenceDto>;

  beforeEach(() => {
    response = new Subject<PagedResultDto<PantryItemDto>>();
    getList = vi.fn(() => response.asObservable());
    preferences = new Subject<UserPreferenceDto>();
    TestBed.configureTestingModule({
      imports: [PantryComponent],
      providers: [
        { provide: PantryService, useValue: { getList } },
        { provide: PreferenceService, useValue: { get: () => preferences.asObservable() } },
        { provide: LIST_QUERY_DEBOUNCE_TIME, useValue: 0 },
      ],
    });
    TestBed.overrideComponent(PantryComponent, {
      set: { imports: [TestLocalizationPipe, TestShortDatePipe] },
    });
    fixture = TestBed.createComponent(PantryComponent);
    fixture.detectChanges();
  });

  afterEach(() => TestBed.resetTestingModule());

  it('muestra carga sin mostrar lista vacía mientras espera', () => {
    expect(fixture.nativeElement.textContent).toContain('::Pantry:Loading');
    expect(fixture.nativeElement.textContent).not.toContain('::Pantry:Empty');
    expect(getList).toHaveBeenCalledWith(expect.anything(), { skipHandleError: true });
  });

  it('muestra lista vacía cuando la consulta termina sin ítems', () => {
    response.next({ items: [], totalCount: 0 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('::Pantry:Empty');
    expect(fixture.nativeElement.textContent).not.toContain('::Pantry:Loading');
  });

  it('muestra resultados y respeta los datos faltantes y la cantidad cero', () => {
    response.next({ items: [{ id: '1', productName: 'Arroz', quantity: 0, unit: null, expirationDate: null, status: PantryItemStatus.Consumed }], totalCount: 1 });
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Arroz');
    expect(text).toContain('0');
    expect(text).toContain('::Pantry:Unavailable');
    expect(text).toContain('::Pantry:Consumed');
    expect(text).not.toContain('::Pantry:Empty');
  });

  it('muestra un error y permite reintentar sin perder la suscripción', () => {
    response.error(new Error('Sin conexión'));
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('::Pantry:Error');
    expect(fixture.nativeElement.textContent).not.toContain('::Pantry:Empty');
    response = new Subject<PagedResultDto<PantryItemDto>>();
    fixture.nativeElement.querySelector('button').click();
    fixture.detectChanges();
    expect(getList).toHaveBeenCalledTimes(2);
    expect(fixture.nativeElement.textContent).toContain('::Pantry:Loading');
    response.next({ items: [], totalCount: 0 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('::Pantry:Empty');
  });

  it('distingue vencido, hoy y próximo a vencer a partir de la respuesta del servidor', () => {
    preferences.next({ showAlerts: true });
    const expired = { ...datedItem, id: 'expired', expirationDate: '2026-11-17', expirationState: ExpirationState.Expired, daysRemaining: -1 };
    const today = { ...datedItem, id: 'today', expirationDate: '2026-11-18', expirationState: ExpirationState.DueToday, daysRemaining: 0 };
    const upcoming = { ...datedItem, id: 'upcoming', expirationDate: '2026-11-21', expirationState: ExpirationState.DueSoon, daysRemaining: 3 };
    const later = { ...datedItem, id: 'later', expirationDate: '2026-11-22', expirationState: ExpirationState.None, daysRemaining: 4 };
    response.next({ items: [expired, today, upcoming, later], totalCount: 4 });
    fixture.detectChanges();

    const cards = fixture.nativeElement.querySelectorAll('.pantry-item') as NodeListOf<HTMLElement>;
    expect(cards[0].querySelector('.expiration-status')?.textContent).toContain('::Pantry:ExpirationExpired');
    expect(cards[1].querySelector('.expiration-status')?.textContent).toContain('::Pantry:ExpirationToday');
    expect(cards[2].querySelector('.expiration-status')?.textContent).toContain('::Pantry:ExpirationUpcoming');
    expect(cards[2].querySelector('time')?.getAttribute('datetime')).toBe('2026-11-21');
    expect(cards[3].querySelector('.expiration-status')).toBeNull();
    expect(fixture.nativeElement.querySelectorAll('.expiration-warning')).toHaveLength(3);
  });

  it('mantiene fecha y estado mientras carga la preferencia y cuando está desactivada', () => {
    response.next({ items: [{ ...datedItem, expirationState: ExpirationState.DueSoon }], totalCount: 1 });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.expiration-warning')).toBeNull();

    preferences.next({ showAlerts: false });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('time')?.textContent).toContain('2026-11-21');
    expect(fixture.nativeElement.textContent).toContain('::Pantry:ExpirationUpcoming');
    expect(fixture.nativeElement.querySelector('.expiration-warning')).toBeNull();
  });

  it('conserva el estado de vencimiento si falla la consulta de preferencias', () => {
    preferences.error(new Error('Sin conexión'));
    response.next({ items: [{ ...datedItem, expirationState: ExpirationState.DueToday }], totalCount: 1 });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('::Pantry:ExpirationToday');
    expect(fixture.nativeElement.querySelector('.expiration-warning')).toBeNull();
  });

  it('omite el aviso para un ítem consumido o sin fecha aunque la respuesta aún lo incluya', () => {
    preferences.next({ showAlerts: true });
    const stale = { ...datedItem, expirationState: ExpirationState.Expired };
    response.next({
      totalCount: 2,
      items: [
        { ...stale, id: 'consumed', status: PantryItemStatus.Consumed },
        { ...stale, id: 'undated', expirationDate: null },
      ],
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.expiration-status')).toBeNull();
    expect(fixture.nativeElement.querySelector('.expiration-warning')).toBeNull();
    expect(fixture.nativeElement.querySelector('time')).not.toBeNull();
  });

  it('tras guardar una nueva fecha deja de mostrar el aviso anterior', () => {
    preferences.next({ showAlerts: true });
    response.next({ items: [{ ...datedItem, expirationState: ExpirationState.DueSoon }], totalCount: 1 });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.expiration-warning')).not.toBeNull();

    response = new Subject<PagedResultDto<PantryItemDto>>();
    fixture.componentInstance.onEditSaved();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.expiration-warning')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('::Pantry:Loading');

    response.next({ items: [{ ...datedItem, expirationDate: '2026-11-22', expirationState: ExpirationState.None }], totalCount: 1 });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('time')?.getAttribute('datetime')).toBe('2026-11-22');
    expect(fixture.nativeElement.querySelector('.expiration-warning')).toBeNull();
  });


  it('solicita la página siguiente cuando hay más resultados', () => {
    response.next({ items: [{ id: '1', productName: 'Arroz' }], totalCount: 11 });
    fixture.detectChanges();
    response = new Subject<PagedResultDto<PantryItemDto>>();
    const buttons = fixture.nativeElement.querySelectorAll('.pagination-controls button');
    expect(buttons[0].disabled).toBe(true);
    buttons[1].click();
    fixture.detectChanges();
    expect(getList).toHaveBeenLastCalledWith(expect.objectContaining({ skipCount: 10, maxResultCount: 10 }), { skipHandleError: true });
    expect(fixture.nativeElement.textContent).toContain('::Pantry:Loading');
  });
});

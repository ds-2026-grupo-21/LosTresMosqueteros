import { LIST_QUERY_DEBOUNCE_TIME } from '@abp/ng.core';
import { Pipe, PipeTransform } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PagedResultDto } from '@abp/ng.core';
import { PantryItemDto, PantryItemStatus, PantryService } from '@proxy/pantry';
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

describe('PantryComponent', () => {
  let fixture: ComponentFixture<PantryComponent>;
  let response: Subject<PagedResultDto<PantryItemDto>>;
  let getList: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    response = new Subject<PagedResultDto<PantryItemDto>>();
    getList = vi.fn(() => response.asObservable());
    TestBed.configureTestingModule({
      imports: [PantryComponent],
      providers: [
        { provide: PantryService, useValue: { getList } },
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

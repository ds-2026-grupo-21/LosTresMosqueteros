import { LIST_QUERY_DEBOUNCE_TIME, PagedResultDto } from '@abp/ng.core';
import { Confirmation, ConfirmationService } from '@abp/ng.theme.shared';
import { CUSTOM_ELEMENTS_SCHEMA, Pipe, PipeTransform } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PantryItemDto, PantryItemStatus, PantryService } from '@proxy/pantry';
import { Subject, of } from 'rxjs';
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

const activeItem: PantryItemDto = { id: 'a1', productName: 'Arroz', status: PantryItemStatus.Active };

describe('PantryComponent acciones', () => {
  let fixture: ComponentFixture<PantryComponent>;
  let component: PantryComponent;
  let getList: ReturnType<typeof vi.fn>;
  let del: ReturnType<typeof vi.fn>;
  let markConsumed: ReturnType<typeof vi.fn>;
  let warn: ReturnType<typeof vi.fn>;

  function setup(confirmStatus: Confirmation.Status) {
    getList = vi.fn(() => of({ items: [activeItem], totalCount: 1 } as PagedResultDto<PantryItemDto>));
    del = vi.fn(() => of(undefined));
    markConsumed = vi.fn(() => of(undefined));
    warn = vi.fn(() => of(confirmStatus));

    TestBed.configureTestingModule({
      imports: [PantryComponent],
      providers: [
        { provide: PantryService, useValue: { getList, delete: del, markConsumed } },
        { provide: ConfirmationService, useValue: { warn } },
        { provide: LIST_QUERY_DEBOUNCE_TIME, useValue: 0 },
      ],
    });
    TestBed.overrideComponent(PantryComponent, {
      set: { imports: [TestLocalizationPipe, TestShortDatePipe], schemas: [CUSTOM_ELEMENTS_SCHEMA] },
    });
    fixture = TestBed.createComponent(PantryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  afterEach(() => TestBed.resetTestingModule());

  describe('eliminación', () => {
    it('vuelve a la página anterior al eliminar el último ítem de la página', () => {
      let deleted = false;
      const lastItem: PantryItemDto = { id: 'last', productName: 'Leche', status: PantryItemStatus.Active };
      getList = vi.fn((query: { skipCount: number }) => of({
        totalCount: deleted ? 10 : 11,
        items: query.skipCount > 0 ? (deleted ? [] : [lastItem]) : [activeItem],
      } as PagedResultDto<PantryItemDto>));
      del = vi.fn(() => { deleted = true; return of(undefined); });
      warn = vi.fn(() => of(Confirmation.Status.confirm));
      TestBed.configureTestingModule({
        imports: [PantryComponent],
        providers: [
          { provide: PantryService, useValue: { getList, delete: del } },
          { provide: ConfirmationService, useValue: { warn } },
          { provide: LIST_QUERY_DEBOUNCE_TIME, useValue: 0 },
        ],
      });
      TestBed.overrideComponent(PantryComponent, {
        set: { imports: [TestLocalizationPipe, TestShortDatePipe], schemas: [CUSTOM_ELEMENTS_SCHEMA] },
      });
      fixture = TestBed.createComponent(PantryComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();
      component.next();
      component.delete(lastItem);
      fixture.detectChanges();
      expect(component.list.page).toBe(0);
      expect(component.items()).toEqual([activeItem]);
      expect(fixture.nativeElement.textContent).not.toContain('::Pantry:Empty');
    });

    it('pide confirmación antes de eliminar', () => {
      setup(Confirmation.Status.confirm);
      component.delete(activeItem);
      expect(warn).toHaveBeenCalledTimes(1);
    });

    it('si el usuario cancela, no llama al servidor y libera el ítem', () => {
      setup(Confirmation.Status.reject);
      component.delete(activeItem);
      expect(del).not.toHaveBeenCalled();
      expect(component.actionBusyId()).toBeNull();
    });

    it('si el usuario confirma, elimina y recarga la lista', () => {
      setup(Confirmation.Status.confirm);
      const callsBefore = getList.mock.calls.length;
      component.delete(activeItem);
      expect(del).toHaveBeenCalledWith('a1', { skipHandleError: true });
      expect(getList.mock.calls.length).toBeGreaterThan(callsBefore);
      expect(component.actionBusyId()).toBeNull();
    });

    it('si el servidor falla, informa el error sin romper la pantalla', () => {
      setup(Confirmation.Status.confirm);
      del.mockReturnValue(new Subject().asObservable());
      const failing = new Subject<void>();
      del.mockReturnValue(failing.asObservable());
      component.delete(activeItem);
      failing.error(new Error('fallo'));
      expect(component.actionError()).toBe('::Pantry:ActionError');
      expect(component.actionBusyId()).toBeNull();
    });
  });

  describe('consumo', () => {
    it('marca como consumido un ítem activo', () => {
      setup(Confirmation.Status.confirm);
      component.markConsumed(activeItem);
      expect(markConsumed).toHaveBeenCalledWith('a1', { skipHandleError: true });
    });

    it('no llama al servidor si el ítem ya está consumido', () => {
      setup(Confirmation.Status.confirm);
      component.markConsumed({ ...activeItem, status: PantryItemStatus.Consumed });
      expect(markConsumed).not.toHaveBeenCalled();
    });
  });

  describe('edición', () => {
    it('abre el modal con el ítem y lo cierra al guardar', () => {
      setup(Confirmation.Status.confirm);
      component.edit(activeItem);
      expect(component.editingItem()).toEqual(activeItem);
      component.onEditSaved();
      expect(component.editingItem()).toBeNull();
    });
  });
});

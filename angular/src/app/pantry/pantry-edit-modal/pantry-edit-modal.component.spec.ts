import { CUSTOM_ELEMENTS_SCHEMA, Pipe, PipeTransform } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { PantryItemDto, PantryService } from '@proxy/pantry';
import { of, Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { PantryEditModalComponent, QUANTITY_MAX, UNIT_MAX_LENGTH } from './pantry-edit-modal.component';

@Pipe({ name: 'abpLocalization' })
class TestLocalizationPipe implements PipeTransform {
  transform(value: string): string { return value; }
}

describe('PantryEditModalComponent', () => {
  let fixture: ComponentFixture<PantryEditModalComponent>;
  let component: PantryEditModalComponent;
  let update: ReturnType<typeof vi.fn>;

  const item: PantryItemDto = {
    id: 'a1', productName: 'Arroz', quantity: 2, unit: 'kg', expirationDate: '2026-12-01',
  };

  beforeEach(() => {
    update = vi.fn(() => of(item));
    TestBed.configureTestingModule({
      imports: [PantryEditModalComponent],
      providers: [{ provide: PantryService, useValue: { update } }],
    });
    TestBed.overrideComponent(PantryEditModalComponent, {
      set: { imports: [ReactiveFormsModule, TestLocalizationPipe], schemas: [CUSTOM_ELEMENTS_SCHEMA] },
    });
    fixture = TestBed.createComponent(PantryEditModalComponent);
    fixture.componentRef.setInput('item', item);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => TestBed.resetTestingModule());

  it('precarga cantidad, unidad y fecha del ítem', () => {
    expect(component.form.getRawValue()).toEqual({ quantity: 2, unit: 'kg', expirationDate: '2026-12-01' });
  });

  it('rechaza una cantidad negativa sin llamar al servidor', () => {
    component.form.patchValue({ quantity: -1 });
    component.save();
    expect(component.form.controls.quantity.errors).toBeTruthy();
    expect(update).not.toHaveBeenCalled();
  });

  it('rechaza una cantidad con más de dos decimales sin llamar al servidor', () => {
    component.form.patchValue({ quantity: 1.234 });
    component.save();
    expect(component.form.controls.quantity.hasError('decimals')).toBe(true);
    expect(update).not.toHaveBeenCalled();
  });

  it.each([0.000000001, 1.000000001, 1.234])('rechaza %s por exceder dos decimales', quantity => {
    component.form.patchValue({ quantity });
    component.save();
    expect(component.form.controls.quantity.hasError('decimals')).toBe(true);
    expect(update).not.toHaveBeenCalled();
  });

  it('acepta una cantidad con dos decimales', () => {
    component.form.patchValue({ quantity: 0.07 });
    expect(component.form.controls.quantity.valid).toBe(true);
  });

  it('rechaza una cantidad por encima del máximo sin llamar al servidor', () => {
    component.form.patchValue({ quantity: QUANTITY_MAX * 10 });
    component.save();
    expect(component.form.controls.quantity.hasError('max')).toBe(true);
    expect(update).not.toHaveBeenCalled();
  });

  it('acepta el máximo de la interfaz y rechaza una cantidad mayor', () => {
    component.form.patchValue({ quantity: QUANTITY_MAX });
    expect(component.form.controls.quantity.valid).toBe(true);
    component.form.patchValue({ quantity: 1000000000000 });
    component.save();
    expect(component.form.controls.quantity.hasError('max')).toBe(true);
    expect(update).not.toHaveBeenCalled();
  });

  it('rechaza una fecha inválida sin llamar al servidor', () => {
    component.form.patchValue({ expirationDate: '2026-13-45' });
    component.save();
    expect(component.form.controls.expirationDate.errors).toBeTruthy();
    expect(update).not.toHaveBeenCalled();
  });

  it.each(['2026-02-30', '2025-02-29', '0000-01-01'])('rechaza la fecha inexistente %s', date => {
    component.form.patchValue({ expirationDate: date });
    component.save();
    expect(component.form.controls.expirationDate.hasError('invalidDate')).toBe(true);
    expect(update).not.toHaveBeenCalled();
  });

  it('acepta el 29 de febrero en un año bisiesto', () => {
    component.form.patchValue({ expirationDate: '2024-02-29' });
    expect(component.form.controls.expirationDate.valid).toBe(true);
  });

  it('rechaza una unidad demasiado larga sin llamar al servidor', () => {
    component.form.patchValue({ unit: 'x'.repeat(UNIT_MAX_LENGTH + 1) });
    component.save();
    expect(update).not.toHaveBeenCalled();
  });

  it('con datos válidos guarda y avisa que se guardó', () => {
    const saved = vi.fn();
    component.saved.subscribe(saved);
    component.form.patchValue({ quantity: 5 });
    component.save();
    expect(update).toHaveBeenCalledWith('a1', expect.objectContaining({ quantity: 5 }), { skipHandleError: true });
    expect(saved).toHaveBeenCalled();
  });

  it('mantiene los datos y permite reintentar si el servidor rechaza la edición', () => {
    const pending = new Subject<PantryItemDto>();
    update.mockReturnValueOnce(pending.asObservable());
    component.form.patchValue({ quantity: 7, unit: 'kg' });
    component.save();
    expect(component.saving()).toBe(true);
    pending.error(new HttpErrorResponse({ status: 400 }));
    expect(component.saving()).toBe(false);
    expect(component.serverError()).toBe('::Pantry:ServerValidationError');
    expect(component.form.getRawValue().quantity).toBe(7);
    expect(component.form.getRawValue().unit).toBe('kg');
  });
});

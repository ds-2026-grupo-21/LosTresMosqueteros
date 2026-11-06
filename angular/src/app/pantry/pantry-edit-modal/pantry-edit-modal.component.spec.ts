import { CUSTOM_ELEMENTS_SCHEMA, Pipe, PipeTransform } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { PantryItemDto, PantryService } from '@proxy/pantry';
import { of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { PantryEditModalComponent, UNIT_MAX_LENGTH } from './pantry-edit-modal.component';

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

  it('rechaza una fecha inválida sin llamar al servidor', () => {
    component.form.patchValue({ expirationDate: '2026-13-45' });
    component.save();
    expect(component.form.controls.expirationDate.errors).toBeTruthy();
    expect(update).not.toHaveBeenCalled();
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
});
import { LocalizationPipe } from '@abp/ng.core';
import { ModalComponent } from '@abp/ng.theme.shared';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, OnInit, inject, input, output, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { PantryItemDto, PantryService, UpdatePantryItemDto } from '@proxy/pantry';

// Debe coincidir con la longitud máxima que valida el servidor (ver DTO de C#).
export const UNIT_MAX_LENGTH = 20;

function validDate(control: AbstractControl): ValidationErrors | null {
  const value = control.value as string | null;
  if (!value) return null;
  const isFormatOk = /^\d{4}-\d{2}-\d{2}$/.test(value);
  const isRealDate = !Number.isNaN(new Date(`${value}T00:00:00`).getTime());
  return isFormatOk && isRealDate ? null : { invalidDate: true };
}

@Component({
  selector: 'app-pantry-edit-modal',
  imports: [ReactiveFormsModule, LocalizationPipe, ModalComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <abp-modal [visible]="true" (visibleChange)="onVisibleChange($event)" [busy]="saving()">
      <ng-template #abpHeader>
        <h3>{{ '::Pantry:EditTitle' | abpLocalization }}</h3>
      </ng-template>

      <ng-template #abpBody>
        <p class="fw-bold">{{ item().productName }}</p>

        @if (serverError()) {
          <div class="alert alert-danger" role="alert">{{ serverError()! | abpLocalization }}</div>
        }

        <form [formGroup]="form" (ngSubmit)="save()" novalidate>
          <div class="mb-3">
            <label class="form-label" for="quantity">{{ '::Pantry:Quantity' | abpLocalization }}</label>
            <input id="quantity" type="number" step="any" min="0" class="form-control" formControlName="quantity" />
            @if (form.controls.quantity.touched && form.controls.quantity.errors) {
              <div class="text-danger small">{{ '::Pantry:QuantityInvalid' | abpLocalization }}</div>
            }
          </div>

          <div class="mb-3">
            <label class="form-label" for="unit">{{ '::Pantry:Unit' | abpLocalization }}</label>
            <input id="unit" type="text" class="form-control" formControlName="unit" />
            @if (form.controls.unit.touched && form.controls.unit.errors) {
              <div class="text-danger small">{{ '::Pantry:UnitTooLong' | abpLocalization }}</div>
            }
          </div>

          <div class="mb-3">
            <label class="form-label" for="expirationDate">{{ '::Pantry:ExpirationDate' | abpLocalization }}</label>
            <input id="expirationDate" type="date" class="form-control" formControlName="expirationDate" />
            @if (form.controls.expirationDate.touched && form.controls.expirationDate.errors) {
              <div class="text-danger small">{{ '::Pantry:DateInvalid' | abpLocalization }}</div>
            }
          </div>
        </form>
      </ng-template>

      <ng-template #abpFooter>
        <button type="button" class="btn btn-secondary" [disabled]="saving()" (click)="closed.emit()">
          {{ '::Pantry:Cancel' | abpLocalization }}
        </button>
        <button type="button" class="btn btn-primary" [disabled]="saving()" (click)="save()">
          {{ '::Pantry:Save' | abpLocalization }}
        </button>
      </ng-template>
    </abp-modal>
  `,
})
export class PantryEditModalComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly pantry = inject(PantryService);

  readonly item = input.required<PantryItemDto>();
  readonly saved = output<void>();
  readonly closed = output<void>();

  readonly saving = signal(false);
  readonly serverError = signal<string | null>(null);

  readonly form = this.fb.group({
    quantity: this.fb.control<number | null>(null, [Validators.min(0)]),
    unit: this.fb.control<string | null>(null, [Validators.maxLength(UNIT_MAX_LENGTH)]),
    expirationDate: this.fb.control<string | null>(null, [validDate]),
  });

  ngOnInit(): void {
    const item = this.item();
    this.form.patchValue({
      quantity: item.quantity ?? null,
      unit: item.unit ?? null,
      expirationDate: item.expirationDate ?? null,
    });
  }

  onVisibleChange(visible: boolean): void {
    if (!visible) this.closed.emit();
  }

  save(): void {
    if (this.saving()) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched(); // no se invoca al servidor
      return;
    }

    const value = this.form.getRawValue();
    const body: UpdatePantryItemDto = {
      quantity: value.quantity,
      unit: value.unit?.trim() || null,
      expirationDate: value.expirationDate || null,
    };

    this.saving.set(true);
    this.serverError.set(null);

    // skipHandleError: el error se muestra dentro del modal para no perder lo ingresado.
    this.pantry
      .update(this.item().id!, body, { skipHandleError: true })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => this.saved.emit(),
        error: (err: HttpErrorResponse) =>
          this.serverError.set(err.status === 400 ? '::Pantry:ServerValidationError' : '::Pantry:SaveError'),
      });
  }
}
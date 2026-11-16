import { LocalizationPipe } from '@abp/ng.core';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { FoodField, type FoodWarningDto } from '@proxy/preferences';

@Component({
  selector: 'app-food-warnings',
  imports: [LocalizationPipe],
  templateUrl: './food-warnings.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FoodWarningsComponent {
  /** Evaluación del servidor (abrir, guardar o agregar). null/undefined = sin datos. */
  readonly warnings = input<FoodWarningDto | null | undefined>(null);
  /** true si la evaluación falló: no se debe interpretar como "sin coincidencias". */
  readonly failed = input(false);
  /** Fuente y aclaración informativa. Se pueden ocultar si la pantalla ya las muestra. */
  readonly showFooter = input(true);

  // Apagar las alertas solo oculta lo personalizado. Si el dato no viene, se considera activado.
  readonly personalized = computed(() => this.warnings()?.showAlerts !== false);
  readonly hasPreferences = computed(() => this.warnings()?.hasPreferences === true);
  readonly matches = computed(() => this.warnings()?.matches ?? []);
  readonly unverifiable = computed(() => this.warnings()?.unverifiable ?? []);

  fieldKey(field: FoodField | undefined): string {
    switch (field) {
      case FoodField.Ingredients:
        return '::Warnings:Field:Ingredients';
      case FoodField.Allergens:
        return '::Warnings:Field:Allergens';
      default:
        return '::Warnings:Field:Unknown';
    }
  }
}
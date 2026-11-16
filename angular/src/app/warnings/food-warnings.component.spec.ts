import { Pipe, PipeTransform } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FoodField, type FoodWarningDto } from '@proxy/preferences';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { FoodWarningsComponent } from './food-warnings.component';

@Pipe({ name: 'abpLocalization' })
class TestLocalizationPipe implements PipeTransform {
  transform(value: string): string {
    return value;
  }
}

describe('FoodWarningsComponent', () => {
  let fixture: ComponentFixture<FoodWarningsComponent>;

  const render = (inputs: { warnings?: FoodWarningDto | null; failed?: boolean; showFooter?: boolean }): string => {
    for (const [name, value] of Object.entries(inputs)) {
      fixture.componentRef.setInput(name, value);
    }
    fixture.detectChanges();
    return fixture.nativeElement.textContent as string;
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [FoodWarningsComponent] });
    TestBed.overrideComponent(FoodWarningsComponent, { set: { imports: [TestLocalizationPipe] } });
    fixture = TestBed.createComponent(FoodWarningsComponent);
  });
  afterEach(() => TestBed.resetTestingModule());

  it('con alertas activadas muestra cada coincidencia con su campo', () => {
    const text = render({
      warnings: {
        hasPreferences: true,
        showAlerts: true,
        matches: [
          { preference: 'Maní', field: FoodField.Ingredients },
          { preference: 'Gluten', field: FoodField.Allergens },
        ],
        unverifiable: [],
      },
    });
    expect(text).toContain('::Warnings:MatchTitle');
    expect(text).toContain('Maní');
    expect(text).toContain('::Warnings:Field:Ingredients');
    expect(text).toContain('Gluten');
    expect(text).toContain('::Warnings:Field:Allergens');
  });

  it('con alertas apagadas oculta lo personalizado pero conserva fuente, aclaración y datos ausentes', () => {
    const text = render({
      warnings: {
        hasPreferences: true,
        showAlerts: false,
        matches: [{ preference: 'Maní', field: FoodField.Ingredients }],
        unverifiable: [FoodField.Allergens],
      },
    });
    expect(text).not.toContain('::Warnings:MatchTitle');
    expect(text).not.toContain('Maní');
    expect(text).not.toContain('::Warnings:NoMatches');
    expect(text).not.toContain('::Warnings:NoPreferences');
    expect(text).toContain('::Warnings:Unverifiable');
    expect(text).toContain('::Warnings:Field:Allergens');
    expect(text).toContain('::Catalog:Source');
    expect(text).toContain('::Catalog:Disclaimer');
  });

  it('distingue "sin preferencias" de "sin coincidencias sobre datos disponibles"', () => {
    const sinPreferencias = render({
      warnings: { hasPreferences: false, showAlerts: true, matches: [], unverifiable: [] },
    });
    expect(sinPreferencias).toContain('::Warnings:NoPreferences');
    expect(sinPreferencias).not.toContain('::Warnings:NoMatches');

    const sinCoincidencias = render({
      warnings: { hasPreferences: true, showAlerts: true, matches: [], unverifiable: [] },
    });
    expect(sinCoincidencias).toContain('::Warnings:NoMatches');
    expect(sinCoincidencias).not.toContain('::Warnings:NoPreferences');
    expect(sinCoincidencias).not.toContain('::Warnings:MatchTitle');
  });

  it('una coincidencia con datos incompletos comunica ambas cosas', () => {
    const text = render({
      warnings: {
        hasPreferences: true,
        showAlerts: true,
        matches: [{ preference: 'Peanuts', field: FoodField.Allergens }],
        unverifiable: [FoodField.Ingredients],
      },
    });
    expect(text).toContain('::Warnings:MatchTitle');
    expect(text).toContain('Peanuts');
    expect(text).toContain('::Warnings:Unverifiable');
    expect(text).toContain('::Warnings:Field:Ingredients');
  });

  it('datos ausentes sin coincidencias no se presentan como ausencia de riesgo', () => {
    const text = render({
      warnings: { hasPreferences: true, showAlerts: true, matches: [], unverifiable: [FoodField.Ingredients, FoodField.Allergens] },
    });
    expect(text).toContain('::Warnings:Unverifiable');
    expect(text).not.toContain('::Warnings:NoMatches');
    expect(text).toContain('::Warnings:Field:Ingredients');
    expect(text).toContain('::Warnings:Field:Allergens');
  });

  it('si la evaluación falla muestra el error y no afirma que no hay coincidencias', () => {
    const text = render({
      warnings: { hasPreferences: true, showAlerts: true, matches: [], unverifiable: [] },
      failed: true,
    });
    expect(text).toContain('::Warnings:EvaluationError');
    expect(text).not.toContain('::Warnings:NoMatches');
    expect(text).toContain('::Catalog:Disclaimer');
  });

  it('sin evaluación solo muestra la fuente y la aclaración', () => {
    const text = render({ warnings: null });
    expect(text).not.toContain('::Warnings:');
    expect(text).toContain('::Catalog:Source');
    expect(text).toContain('::Catalog:Disclaimer');
  });

  it('el texto externo se muestra como dato y no se ejecuta como HTML', () => {
    const text = render({
      warnings: {
        hasPreferences: true,
        showAlerts: true,
        matches: [{ preference: '<img src=x onerror=alert(1)>', field: FoodField.Ingredients }],
        unverifiable: [],
      },
    });
    expect(text).toContain('<img src=x onerror=alert(1)>');
    expect(fixture.nativeElement.querySelector('img')).toBeNull();
  });

  it('permite ocultar la fuente y la aclaración cuando la pantalla ya las muestra', () => {
    const text = render({
      warnings: { hasPreferences: true, showAlerts: true, matches: [], unverifiable: [] },
      showFooter: false,
    });
    expect(text).not.toContain('::Catalog:Source');
    expect(text).not.toContain('::Catalog:Disclaimer');
  });
});

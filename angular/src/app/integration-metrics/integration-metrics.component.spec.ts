import { DatePipe, DecimalPipe } from '@angular/common';
import { Pipe, PipeTransform } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ExternalApiOperation, IntegrationMetricsDto, IntegrationMetricsService } from '@proxy/integrations';
import { Subject, of, throwError } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { IntegrationMetricsComponent } from './integration-metrics.component';

@Pipe({ name: 'abpLocalization' })
class TestLocalizationPipe implements PipeTransform {
  transform(value: string): string { return value; }
}

const empty: IntegrationMetricsDto = {
  days: 7,
  totalCalls: 0,
  operations: [
    { operation: ExternalApiOperation.BarcodeLookup, totalCalls: 0 },
    { operation: ExternalApiOperation.TextSearch, totalCalls: 0 },
  ],
};

const withData: IntegrationMetricsDto = {
  days: 7,
  totalCalls: 5,
  operations: [
    {
      operation: ExternalApiOperation.BarcodeLookup, totalCalls: 4, found: 1, notFound: 1, rateLimited: 1,
      unavailable: 1, errors: 0, failureRatePercent: 50, highFailureRate: true, averageDurationMs: 362.5,
      maxDurationMs: 1000, lastCallAt: '2026-11-21T12:00:00Z',
    },
    {
      operation: ExternalApiOperation.TextSearch, totalCalls: 1, found: 1, notFound: 0, rateLimited: 0,
      unavailable: 0, errors: 0, failureRatePercent: 0, highFailureRate: false, averageDurationMs: 200,
      maxDurationMs: 200, lastCallAt: '2026-11-21T12:00:00Z',
    },
  ],
};

describe('IntegrationMetricsComponent', () => {
  let fixture: ComponentFixture<IntegrationMetricsComponent>;
  let get: ReturnType<typeof vi.fn>;

  function setup(impl: () => unknown) {
    get = vi.fn(impl);
    TestBed.configureTestingModule({
      imports: [IntegrationMetricsComponent],
      providers: [{ provide: IntegrationMetricsService, useValue: { get } }],
    });
    TestBed.overrideComponent(IntegrationMetricsComponent, {
      set: { imports: [TestLocalizationPipe, DatePipe, DecimalPipe] },
    });
    fixture = TestBed.createComponent(IntegrationMetricsComponent);
    fixture.detectChanges();
  }

  const text = () => fixture.nativeElement.textContent as string;
  const query = (selector: string) => fixture.nativeElement.querySelector(selector) as HTMLElement | null;

  afterEach(() => TestBed.resetTestingModule());

  it('muestra carga y pide los últimos 7 días sin el manejo de errores global', () => {
    setup(() => new Subject<IntegrationMetricsDto>().asObservable());
    expect(text()).toContain('::Metrics:Loading');
    expect(get).toHaveBeenCalledWith({ days: 7 }, { skipHandleError: true });
  });

  it('muestra el estado vacío cuando no hubo llamadas', () => {
    setup(() => of(empty));
    expect(text()).toContain('::Metrics:Empty');
    expect(query('[data-test="operation-1"]')).toBeNull();
  });

  it('muestra las métricas por operación y avisa fallas y límite de solicitudes', () => {
    setup(() => of(withData));
    expect(text()).toContain('::Metrics:Operation:BarcodeLookup');
    expect(text()).toContain('::Metrics:Operation:TextSearch');
    expect(query('[data-test="operation-1"] [data-test="high-failure"]')).not.toBeNull();
    expect(query('[data-test="operation-1"] [data-test="rate-limited"]')).not.toBeNull();
    expect(query('[data-test="operation-2"] [data-test="high-failure"]')).toBeNull();
    expect(query('[data-test="operation-2"] [data-test="rate-limited"]')).toBeNull();
    expect(text()).toContain('1000 ms');
  });

  it('muestra el error y permite reintentar', () => {
    setup(() => throwError(() => new Error('sin conexión')));
    expect(text()).toContain('::Metrics:Error');

    get.mockReturnValue(of(withData));
    query('[data-test="retry"]')!.click();
    fixture.detectChanges();

    expect(get).toHaveBeenCalledTimes(2);
    expect(text()).toContain('::Metrics:Operation:BarcodeLookup');
  });

  it('al cambiar el período consulta de nuevo con los días elegidos', () => {
    setup(() => of(withData));
    query('[data-test="period-30"]')!.click();
    fixture.detectChanges();
    expect(get).toHaveBeenLastCalledWith({ days: 30 }, { skipHandleError: true });
  });

  it('una respuesta anterior no reemplaza la del período más reciente', () => {
    const slow = new Subject<IntegrationMetricsDto>();
    const fast = new Subject<IntegrationMetricsDto>();
    setup(() => slow.asObservable());
    get.mockReturnValue(fast.asObservable());

    query('[data-test="period-30"]')!.click();
    fast.next({ ...empty, days: 30 });
    slow.next(withData);
    fixture.detectChanges();

    expect(text()).toContain('::Metrics:Empty');
    expect(text()).not.toContain('::Metrics:Operation:BarcodeLookup');
  });
});

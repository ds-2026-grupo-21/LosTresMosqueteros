import { LocalizationPipe } from '@abp/ng.core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  ExternalApiOperation,
  IntegrationMetricsDto,
  IntegrationMetricsService,
  OperationMetricsDto,
} from '@proxy/integrations';
import { Subscription } from 'rxjs';

type ViewState = 'loading' | 'error' | 'success';

export const PERIOD_OPTIONS = [1, 7, 30] as const;

@Component({
  selector: 'app-integration-metrics',
  imports: [LocalizationPipe, DatePipe, DecimalPipe],
  templateUrl: './integration-metrics.component.html',
  styleUrl: './integration-metrics.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IntegrationMetricsComponent implements OnInit {
  private readonly service = inject(IntegrationMetricsService);
  private readonly destroyRef = inject(DestroyRef);
  private request?: Subscription;

  readonly periods = PERIOD_OPTIONS;
  readonly days = signal<number>(7);
  readonly state = signal<ViewState>('loading');
  readonly metrics = signal<IntegrationMetricsDto | null>(null);

  ngOnInit(): void {
    this.load();
  }

  selectPeriod(days: number): void {
    if (days === this.days() && this.state() !== 'error') return;
    this.days.set(days);
    this.load();
  }

  load(): void {
    // Una respuesta anterior no reemplaza la de un período más reciente.
    this.request?.unsubscribe();
    this.state.set('loading');
    this.request = this.service
      .get({ days: this.days() }, { skipHandleError: true })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: metrics => {
          this.metrics.set(metrics);
          this.state.set('success');
        },
        error: () => this.state.set('error'),
      });
  }

  operationKey(operation?: ExternalApiOperation): string {
    return operation === ExternalApiOperation.TextSearch
      ? '::Metrics:Operation:TextSearch'
      : '::Metrics:Operation:BarcodeLookup';
  }

  periodKey(days: number): string {
    return `::Metrics:LastDays:${days}`;
  }

  hasCalls(operation: OperationMetricsDto): boolean {
    return (operation.totalCalls ?? 0) > 0;
  }
}

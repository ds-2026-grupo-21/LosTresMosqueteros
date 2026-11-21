import type { GetIntegrationMetricsInput, IntegrationMetricsDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class IntegrationMetricsService {
  private restService = inject(RestService);
  apiName = 'Default';


  get = (input: GetIntegrationMetricsInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, IntegrationMetricsDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/integration-metrics',
      params: { days: input.days },
    },
    { apiName: this.apiName,...config });
}

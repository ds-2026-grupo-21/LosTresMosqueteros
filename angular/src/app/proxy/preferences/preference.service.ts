import type { UpdateUserPreferenceDto, UserPreferenceDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class PreferenceService {
  private restService = inject(RestService);
  apiName = 'Default';


  get = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, UserPreferenceDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/preference',
    },
    { apiName: this.apiName,...config });


  update = (input: UpdateUserPreferenceDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, UserPreferenceDto>({
      method: 'PUT',
      headers: { Accept: 'application/json' },
      url: '/api/app/preference',
      body: input,
    },
    { apiName: this.apiName,...config });
}

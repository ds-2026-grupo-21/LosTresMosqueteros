import type { CreatePantryItemDto, PantryItemDto, UpdatePantryItemDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto, PagedResultRequestDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class PantryService {
  private restService = inject(RestService);
  apiName = 'Default';


  create = (input: CreatePantryItemDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PantryItemDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: '/api/app/pantry',
      body: input,
    },
    { apiName: this.apiName,...config });


  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/pantry/${id}`,
    },
    { apiName: this.apiName,...config });


  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PantryItemDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: `/api/app/pantry/${id}`,
    },
    { apiName: this.apiName,...config });


  getList = (input: PagedResultRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<PantryItemDto>>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/pantry',
      params: { skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });


  markConsumed = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/pantry/${id}/mark-consumed`,
    },
    { apiName: this.apiName,...config });


  update = (id: string, input: UpdatePantryItemDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PantryItemDto>({
      method: 'PUT',
      headers: { Accept: 'application/json' },
      url: `/api/app/pantry/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}
import type { BuscarProductosExternosDto, ConsultarProductoExternoDto, ExternalProductDto, ExternalProductSearchResultDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class ConsultaProductoExternoService {
  private restService = inject(RestService);
  apiName = 'Default';


  get = (input: ConsultarProductoExternoDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ExternalProductDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/consulta-producto-externo',
      params: { codigoBarras: input.codigoBarras },
    },
    { apiName: this.apiName,...config });


  search = (input: BuscarProductosExternosDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ExternalProductSearchResultDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: '/api/app/consulta-producto-externo/search',
      body: input,
    },
    { apiName: this.apiName,...config });
}

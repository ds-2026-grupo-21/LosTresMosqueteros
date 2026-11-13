import type { ConsultarProductoExternoDto, GuardarProductoExternoResultadoDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class GuardarProductoExternoService {
  private restService = inject(RestService);
  apiName = 'Default';


  save = (input: ConsultarProductoExternoDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, GuardarProductoExternoResultadoDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: '/api/app/guardar-producto-externo/save',
      body: input,
    },
    { apiName: this.apiName,...config });
}

import { mapEnumToOptions } from '@abp/ng.core';

export enum ResultadoConsultaExterna {
  Encontrado = 0,
  NoEncontrado = 1,
  LimiteDeSolicitudesExcedido = 2,
  ServicioNoDisponible = 3,
}

export const resultadoConsultaExternaOptions = mapEnumToOptions(ResultadoConsultaExterna);

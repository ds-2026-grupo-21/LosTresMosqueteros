import type { ResultadoConsultaExterna } from './resultado-consulta-externa.enum';
import type { FullAuditedEntityDto } from '@abp/ng.core';

export interface ConsultarProductoExternoDto {
  codigoBarras: string;
}

export interface CreateProductoDto {
  codigoBarras: string;
  nombre: string;
  ingredientes?: string | null;
  alergenos?: string | null;
}

export interface ExternalProductDto {
  resultado?: ResultadoConsultaExterna;
  codigoBarras?: string | null;
  nombre?: string | null;
  marca?: string | null;
  imagenUrl?: string | null;
}

export interface ProductoDto extends FullAuditedEntityDto<string> {
  codigoBarras?: string;
  nombre?: string;
  ingredientes?: string | null;
  alergenos?: string | null;
}

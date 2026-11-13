import type { ResultadoConsultaExterna } from './resultado-consulta-externa.enum';
import type { FullAuditedEntityDto } from '@abp/ng.core';

export interface BuscarProductosExternosDto {
  texto: string;
  pagina?: number;
  tamanoPagina?: number;
}

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
  categorias?: string | null;
  ingredientes?: string | null;
  alergenos?: string[] | null;
  nutriScore?: string | null;
  grupoNova?: number | null;
}

export interface ExternalProductSearchResultDto {
  resultado?: ResultadoConsultaExterna;
  pagina?: number;
  tamanoPagina?: number;
  totalResultados?: number | null;
  totalEsExacto?: boolean | null;
  paginasDisponibles?: number | null;
  productos?: ExternalProductSummaryDto[];
}

export interface ExternalProductSummaryDto {
  codigoBarras?: string;
  nombre?: string | null;
  marca?: string | null;
  imagenUrl?: string | null;
}

export interface GuardarProductoExternoResultadoDto {
  resultado?: ResultadoConsultaExterna;
  producto?: ProductoDto | null;
}

export interface ProductoDto extends FullAuditedEntityDto<string> {
  codigoBarras?: string;
  nombre?: string | null;
  marca?: string | null;
  imagenUrl?: string | null;
  categorias?: string | null;
  nutriScore?: string | null;
  grupoNova?: number | null;
  ingredientes?: string | null;
  alergenos?: string | null;
}

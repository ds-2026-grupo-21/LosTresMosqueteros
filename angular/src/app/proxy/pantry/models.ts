import type { EntityDto } from '@abp/ng.core';
import type { PantryItemStatus } from './pantry-item-status.enum';

export interface CreatePantryItemDto extends UpdatePantryItemDto {
  productId?: string;
}

export interface PantryItemDto extends EntityDto<string> {
  productId?: string;
  productName?: string | null;
  status?: PantryItemStatus;
  quantity?: number | null;
  unit?: string | null;
  expirationDate?: string | null;
}

export interface UpdatePantryItemDto {
  quantity?: number | null;
  unit?: string | null;
  expirationDate?: string | null;
}

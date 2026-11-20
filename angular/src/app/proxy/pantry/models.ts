import type { EntityDto } from '@abp/ng.core';
import type { ExpirationState } from './expiration-state.enum';
import type { PantryItemStatus } from './pantry-item-status.enum';
import type { FoodWarningDto } from '../preferences/models';

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
  expirationState?: ExpirationState;
  daysRemaining?: number | null;
  advertencias?: FoodWarningDto | null;
}

export interface UpdatePantryItemDto {
  quantity?: number | null;
  unit?: string | null;
  expirationDate?: string | null;
}

import type { FoodField } from './food-field.enum';

export interface FoodWarningDto {
  hasPreferences?: boolean;
  showAlerts?: boolean;
  matches?: FoodWarningMatchDto[];
  unverifiable?: FoodField[];
}

export interface FoodWarningMatchDto {
  preference?: string;
  field?: FoodField;
}

export interface UpdateUserPreferenceDto {
  showAlerts?: boolean;
  photoDataUrl?: string | null;
  watchedItems?: string[];
}

export interface UserPreferenceDto {
  showAlerts?: boolean;
  photoDataUrl?: string | null;
  watchedItems?: string[];
}

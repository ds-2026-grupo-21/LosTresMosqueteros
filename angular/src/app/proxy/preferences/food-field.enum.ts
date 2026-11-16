import { mapEnumToOptions } from '@abp/ng.core';

export enum FoodField {
  Ingredients = 1,
  Allergens = 2,
}

export const foodFieldOptions = mapEnumToOptions(FoodField);

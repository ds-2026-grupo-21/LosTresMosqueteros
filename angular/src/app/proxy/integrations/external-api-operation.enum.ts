import { mapEnumToOptions } from '@abp/ng.core';

export enum ExternalApiOperation {
  BarcodeLookup = 1,
  TextSearch = 2,
}

export const externalApiOperationOptions = mapEnumToOptions(ExternalApiOperation);

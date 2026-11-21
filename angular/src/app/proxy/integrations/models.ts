import type { ExternalApiOperation } from './external-api-operation.enum';

export interface GetIntegrationMetricsInput {
  days?: number;
}

export interface IntegrationMetricsDto {
  from?: string;
  to?: string;
  days?: number;
  totalCalls?: number;
  operations?: OperationMetricsDto[];
}

export interface OperationMetricsDto {
  operation?: ExternalApiOperation;
  totalCalls?: number;
  found?: number;
  notFound?: number;
  rateLimited?: number;
  unavailable?: number;
  errors?: number;
  failureRatePercent?: number;
  highFailureRate?: boolean;
  averageDurationMs?: number | null;
  maxDurationMs?: number | null;
  lastCallAt?: string | null;
}

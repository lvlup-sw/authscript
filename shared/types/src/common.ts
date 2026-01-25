/**
 * Common type definitions shared across the platform
 */

export type SortDirection = 'asc' | 'desc';

export interface PaginationParams {
  pageNumber: number;
  pageSize: number;
  sortDirection?: SortDirection;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface ApiError {
  type: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
}

export interface ApiResponse<T> {
  data?: T;
  error?: ApiError;
  success: boolean;
}

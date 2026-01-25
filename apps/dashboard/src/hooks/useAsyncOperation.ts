import { useState, useCallback } from 'react';
import { toast } from 'sonner';

interface UseAsyncOperationOptions {
  onSuccess?: (result?: any) => void;
  onError?: (error: any) => void;
  successMessage?: string;
  errorMessage?: string;
  showSuccessToast?: boolean;
  showErrorToast?: boolean;
  logErrors?: boolean;
}

interface AsyncOperationState {
  isLoading: boolean;
  error: any;
  data: any;
}

interface UseAsyncOperationReturn<T = any> {
  execute: (operation: () => Promise<T>) => Promise<T | undefined>;
  isLoading: boolean;
  error: any;
  data: T | undefined;
  reset: () => void;
}

/**
 * Custom hook for managing async operations with centralized loading, error handling, and success feedback
 * 
 * @example
 * ```tsx
 * const { execute, isLoading, error } = useAsyncOperation({
 *   successMessage: 'Session created successfully!',
 *   errorMessage: 'Failed to create session',
 *   onSuccess: (result) => navigate(`/sessions/${result.id}`)
 * });
 * 
 * const handleCreateSession = () => {
 *   execute(async () => {
 *     return await createSession.mutateAsync({ patientId, practitionerId });
 *   });
 * };
 * ```
 */
export function useAsyncOperation<T = any>(options: UseAsyncOperationOptions = {}): UseAsyncOperationReturn<T> {
  const {
    onSuccess,
    onError,
    successMessage,
    errorMessage,
    showSuccessToast = true,
    showErrorToast = true,
    logErrors = true
  } = options;

  const [state, setState] = useState<AsyncOperationState>({
    isLoading: false,
    error: null,
    data: undefined
  });

  const execute = useCallback(async (operation: () => Promise<T>): Promise<T | undefined> => {
    setState(prev => ({ ...prev, isLoading: true, error: null }));

    try {
      const result = await operation();
      
      setState(prev => ({ 
        ...prev, 
        isLoading: false, 
        data: result, 
        error: null 
      }));

      // Success handling
      if (successMessage && showSuccessToast) {
        toast.success(successMessage);
      }

      if (onSuccess) {
        onSuccess(result);
      }

      return result;
    } catch (error: any) {
      setState(prev => ({ 
        ...prev, 
        isLoading: false, 
        error 
      }));

      // Error handling
      if (logErrors) {
        console.error('Async operation failed:', error);
      }

      if (errorMessage && showErrorToast) {
        toast.error(errorMessage);
      } else if (showErrorToast && error?.message) {
        toast.error(error.message);
      }

      if (onError) {
        onError(error);
      }

      return undefined;
    }
  }, [onSuccess, onError, successMessage, errorMessage, showSuccessToast, showErrorToast, logErrors]);

  const reset = useCallback(() => {
    setState({
      isLoading: false,
      error: null,
      data: undefined
    });
  }, []);

  return {
    execute,
    isLoading: state.isLoading,
    error: state.error,
    data: state.data,
    reset
  };
}

/**
 * Simplified version for operations that don't need complex state management
 * Just handles loading state and basic error/success feedback
 */
export function useSimpleAsyncOperation(options: Omit<UseAsyncOperationOptions, 'onSuccess'> & { 
  onSuccess?: () => void 
} = {}) {
  const [isLoading, setIsLoading] = useState(false);

  const execute = useCallback(async (operation: () => Promise<any>): Promise<boolean> => {
    if (isLoading) return false;
    
    setIsLoading(true);
    
    try {
      await operation();
      
      if (options.successMessage && options.showSuccessToast !== false) {
        toast.success(options.successMessage);
      }
      
      if (options.onSuccess) {
        options.onSuccess();
      }
      
      return true;
    } catch (error: any) {
      if (options.logErrors !== false) {
        console.error('Operation failed:', error);
      }
      
      if (options.errorMessage && options.showErrorToast !== false) {
        toast.error(options.errorMessage);
      } else if (options.showErrorToast !== false && error?.message) {
        toast.error(error.message);
      }
      
      if (options.onError) {
        options.onError(error);
      }
      
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [isLoading, options]);

  return { execute, isLoading };
}
// Shared Custom Hooks - Following Single Responsibility Principle
// Each hook has one clear purpose and can be reused across the application

// API and Data Management Hooks
export { useApi } from './useApi';
export type { ApiResponse } from './useApi';

export { useApiWithErrorHandling, useFormSubmission, usePaginatedApiWithErrorHandling } from './useApiWithErrorHandling';

export { usePagination } from './usePagination';
export { useAsync, useAsyncOperations } from './useAsync';

// State Management Hooks
export { useLocalStorage, useUserPreferences } from './useLocalStorage';

// Performance and UX Hooks
export { useDebounce, useDebouncedCallback, useDebouncedSearch } from './useDebounce';

// Form Management Hooks
export { useForm } from './useForm';

// Notification and Error Handling Hooks
export { useNotifications, useFormNotifications } from './useNotifications';
export type { Notification, NotificationType } from './useNotifications';
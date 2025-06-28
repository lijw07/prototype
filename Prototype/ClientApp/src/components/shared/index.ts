// Shared UI Components - Following Single Responsibility Principle
// Each component has one clear purpose and can be reused across the application

// Display Components
export { default as LoadingSpinner } from './LoadingSpinner';
export { default as ErrorBoundary } from './ErrorBoundary';
export { default as Alert } from './Alert';
export { default as NotificationContainer } from './NotificationContainer';

// Data Components
export { default as DataTable } from './DataTable';
export type { Column } from './DataTable';
export { default as Pagination } from './Pagination';

// Form Components
export { default as FormInput } from './FormInput';
export { default as FormSelect } from './FormSelect';

// Button component removed - using native HTML buttons with consistent styling
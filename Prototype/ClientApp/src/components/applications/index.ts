// Applications module exports - provides clean interface for consuming components
// Follows clean architecture by exposing only necessary components and hooks

// Import components and hooks for grouped exports
import ApplicationsDashboard from './ApplicationsDashboard';
import ApplicationsList from './ApplicationsList';
import ApplicationForm from './ApplicationForm';
import ApplicationSearchFilters from './ApplicationSearchFilters';
import ApplicationDeleteModal from './ApplicationDeleteModal';
import ConnectionTestPanel from './ConnectionTestPanel';
import ApplicationsPagination from './ApplicationsPagination';
import { useApplications } from './hooks/useApplications';
import { useApplicationForm } from './hooks/useApplicationForm';
import { useConnectionTest } from './hooks/useConnectionTest';

// Main Dashboard Component
export { default as ApplicationsDashboard } from './ApplicationsDashboard';

// Individual UI Components
export { default as ApplicationsList } from './ApplicationsList';
export { default as ApplicationForm } from './ApplicationForm';
export { default as ApplicationSearchFilters } from './ApplicationSearchFilters';
export { default as ApplicationDeleteModal } from './ApplicationDeleteModal';
export { default as ConnectionTestPanel } from './ConnectionTestPanel';
export { default as ApplicationsPagination } from './ApplicationsPagination';

// Custom Hooks
export { useApplications } from './hooks/useApplications';
export { useApplicationForm } from './hooks/useApplicationForm';
export { useConnectionTest } from './hooks/useConnectionTest';

// Types
export type { ApplicationsOverview, ApplicationsState } from './hooks/useApplications';
export type { ApplicationFormData, FormValidationErrors, FormState } from './hooks/useApplicationForm';
export type { ConnectionTestResult, ConnectionTestState } from './hooks/useConnectionTest';

// Enums and Utilities
export { DataSourceTypeEnum, getDataSourceTypeName } from './hooks/useApplicationForm';

// Grouped exports for convenience
export const ApplicationsComponents = {
  Dashboard: ApplicationsDashboard,
  List: ApplicationsList,
  Form: ApplicationForm,
  SearchFilters: ApplicationSearchFilters,
  DeleteModal: ApplicationDeleteModal,
  ConnectionTestPanel: ConnectionTestPanel,
  Pagination: ApplicationsPagination
};

export const ApplicationsHooks = {
  useApplications,
  useApplicationForm,
  useConnectionTest
};
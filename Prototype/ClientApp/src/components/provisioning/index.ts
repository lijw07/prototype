// User Provisioning Components - Decomposed and organized following SRP
// Centralized exports for all provisioning-related components

// Main Dashboard Component
export { default as UserProvisioningDashboard } from './UserProvisioningDashboard';

// Individual Feature Components
export { default as ProvisioningOverview } from './ProvisioningOverview';
export { default as PendingRequestsTable } from './PendingRequestsTable';
export { default as BulkUploadForm } from './BulkUploadForm';
export { default as AutoProvisioningPanel } from './AutoProvisioningPanel';
export { default as MigrationProgressTracker } from './MigrationProgressTracker';

// Custom Hooks
export { useProvisioning } from './hooks/useProvisioning';
export { useBulkUpload } from './hooks/useBulkUpload';
export { useSignalRProgress } from './hooks/useSignalRProgress';

// Type Definitions
export * from './types/provisioning.types';

// Import hooks for grouped exports
import { useProvisioning } from './hooks/useProvisioning';
import { useBulkUpload } from './hooks/useBulkUpload';
import { useSignalRProgress } from './hooks/useSignalRProgress';

// Import components for grouped exports
import UserProvisioningDashboard from './UserProvisioningDashboard';
import ProvisioningOverview from './ProvisioningOverview';
import PendingRequestsTable from './PendingRequestsTable';
import BulkUploadForm from './BulkUploadForm';
import AutoProvisioningPanel from './AutoProvisioningPanel';
import MigrationProgressTracker from './MigrationProgressTracker';

// Component Groups for easy imports
export const ProvisioningComponents = {
  Dashboard: UserProvisioningDashboard,
  Overview: ProvisioningOverview,
  PendingRequests: PendingRequestsTable,
  BulkUpload: BulkUploadForm,
  AutoProvisioning: AutoProvisioningPanel,
  MigrationProgress: MigrationProgressTracker
};

export const ProvisioningHooks = {
  useProvisioning,
  useBulkUpload,
  useSignalRProgress
};
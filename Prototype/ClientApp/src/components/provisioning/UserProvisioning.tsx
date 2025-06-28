// User Provisioning Component - Transition wrapper
// This component now uses the decomposed UserProvisioningDashboard for better maintainability
// Maintains backward compatibility with existing routing and imports

import React from 'react';
import UserProvisioningDashboard from './UserProvisioningDashboard';

/**
 * UserProvisioning Component
 * 
 * This component has been refactored to use the new decomposed architecture.
 * It now serves as a compatibility wrapper around UserProvisioningDashboard,
 * which contains all the functionality split into focused, single-responsibility components.
 * 
 * Decomposed Components:
 * - ProvisioningOverview: Dashboard metrics and statistics
 * - PendingRequestsTable: Manages pending provisioning requests  
 * - BulkUploadForm: File upload interface with drag-and-drop
 * - AutoProvisioningPanel: Auto-provisioning configuration
 * - MigrationProgressTracker: Real-time progress display
 * - UserProvisioningDashboard: Main container component
 * 
 * Benefits of the new architecture:
 * - Single Responsibility Principle (SRP) compliance
 * - Better separation of concerns
 * - Improved maintainability and testability
 * - Reusable components
 * - Enhanced type safety with TypeScript
 * - Real-time updates via SignalR integration
 */
export default function UserProvisioning() {
  return (
    <div className="container-fluid px-4 py-3">
      <UserProvisioningDashboard 
        defaultView="grid"
        enableNotifications={true}
        className="w-100"
      />
    </div>
  );
}
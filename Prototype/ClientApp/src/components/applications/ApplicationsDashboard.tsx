// ApplicationsDashboard component - main container that orchestrates all Applications functionality
// Follows clean architecture by composing smaller components and managing application state

import React, { useEffect, useState } from 'react';
import { Plus, Database } from 'lucide-react';
import { useApplications } from './hooks/useApplications';
import { useApplicationForm } from './hooks/useApplicationForm';
import { useConnectionTest } from './hooks/useConnectionTest';
import ApplicationsList from './ApplicationsList';
import ApplicationSearchFilters from './ApplicationSearchFilters';
import ApplicationForm from './ApplicationForm';
import ApplicationDeleteModal from './ApplicationDeleteModal';
import ConnectionTestPanel from './ConnectionTestPanel';
import ApplicationsPagination from './ApplicationsPagination';
import type { Application } from '../../types/api.types';

export const ApplicationsDashboard: React.FC = () => {
  // Custom hooks for business logic
  const applicationsHook = useApplications();
  const formHook = useApplicationForm();
  const connectionTestHook = useConnectionTest();

  // Local UI state
  const [showForm, setShowForm] = useState(false);
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [applicationToDelete, setApplicationToDelete] = useState<Application | null>(null);

  // Initialize data on component mount
  useEffect(() => {
    applicationsHook.fetchApplications();
    applicationsHook.fetchAllApplications();
  }, []);

  // Handle form submission
  const handleFormSubmit = async (formData: any) => {
    try {
      let result;
      if (formHook.isEditing) {
        result = await applicationsHook.updateApplication(formHook.editingApp!.applicationId, formData);
      } else {
        result = await applicationsHook.createApplication(formData);
      }

      if (result.success) {
        setShowForm(false);
        formHook.resetForm();
        formHook.setSubmitSuccess(true);
      }
    } catch (error) {
      console.error('Form submission error:', error);
    }
  };

  // Handle edit action
  const handleEdit = (application: Application) => {
    formHook.loadApplication(application);
    setShowForm(true);
  };

  // Handle delete action
  const handleDelete = (application: Application) => {
    setApplicationToDelete(application);
    setShowDeleteModal(true);
  };

  // Handle delete confirmation
  const handleDeleteConfirm = async (application: Application) => {
    const result = await applicationsHook.deleteApplication(application.applicationId);
    if (result.success) {
      setShowDeleteModal(false);
      setApplicationToDelete(null);
    }
  };

  // Handle new application
  const handleNewApplication = () => {
    formHook.resetForm();
    setShowForm(true);
  };

  // Handle form close
  const handleFormClose = () => {
    setShowForm(false);
    formHook.resetForm();
  };

  // Handle delete modal close
  const handleDeleteModalClose = () => {
    setShowDeleteModal(false);
    setApplicationToDelete(null);
  };

  // Handle connection test
  const handleTestConnection = async (application: Application) => {
    await connectionTestHook.testConnection(application);
  };

  // Handle test all connections
  const handleTestAllConnections = async () => {
    await connectionTestHook.testMultipleConnections(applicationsHook.applications);
  };

  // Handle filter changes
  const handleSearchChange = (searchTerm: string) => {
    applicationsHook.updateFilters({ searchTerm });
  };

  const handleConnectionTypeChange = (connectionType: string) => {
    applicationsHook.updateFilters({ connectionType });
  };

  const handleAuthTypeChange = (authType: string) => {
    applicationsHook.updateFilters({ authType });
  };

  const handleSortOrderChange = (sortOrder: 'newest' | 'oldest') => {
    applicationsHook.updateFilters({ sortOrder });
  };

  const handleClearFilters = () => {
    applicationsHook.updateFilters({
      searchTerm: '',
      connectionType: 'all',
      authType: 'all',
      sortOrder: 'newest'
    });
  };

  // Get filtered applications for display
  const filteredApplications = applicationsHook.getFilteredApplications();
  const hasFilters = applicationsHook.filters.searchTerm !== '' || 
                    applicationsHook.filters.connectionType !== 'all' || 
                    applicationsHook.filters.authType !== 'all' || 
                    applicationsHook.filters.sortOrder !== 'newest';

  return (
    <div className="container-fluid px-4 py-4">
      {/* Header */}
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h2 className="h4 mb-1 d-flex align-items-center">
            <Database size={24} className="me-2 text-primary" />
            Applications Management
          </h2>
          <p className="text-muted mb-0">
            Manage database connections and application integrations
          </p>
        </div>
        <button
          className="btn btn-primary"
          onClick={handleNewApplication}
        >
          <Plus size={18} className="me-2" />
          Add Application
        </button>
      </div>

      {/* Error Display */}
      {applicationsHook.error && (
        <div className="alert alert-danger d-flex align-items-center justify-content-between mb-4">
          <span>{applicationsHook.error}</span>
          <button
            className="btn btn-sm btn-outline-danger"
            onClick={applicationsHook.clearError}
          >
            Dismiss
          </button>
        </div>
      )}

      {/* Connection Testing Panel */}
      <ConnectionTestPanel
        testing={connectionTestHook.testing}
        lastTest={connectionTestHook.lastTest}
        error={connectionTestHook.error}
        hasResults={connectionTestHook.hasResults}
        resultCount={connectionTestHook.resultCount}
        onTestAll={handleTestAllConnections}
        onClearResults={connectionTestHook.clearTestResults}
        onClearError={connectionTestHook.clearError}
        getTestStatistics={connectionTestHook.getTestStatistics}
      />

      {/* Search and Filters */}
      <ApplicationSearchFilters
        searchTerm={applicationsHook.filters.searchTerm}
        connectionType={applicationsHook.filters.connectionType}
        authType={applicationsHook.filters.authType}
        sortOrder={applicationsHook.filters.sortOrder}
        onSearchChange={handleSearchChange}
        onConnectionTypeChange={handleConnectionTypeChange}
        onAuthTypeChange={handleAuthTypeChange}
        onSortOrderChange={handleSortOrderChange}
        onClearFilters={handleClearFilters}
        totalCount={applicationsHook.allApplications.length}
        filteredCount={filteredApplications.length}
      />

      {/* Applications List */}
      <ApplicationsList
        applications={hasFilters ? filteredApplications : applicationsHook.applications}
        loading={applicationsHook.loading}
        onEdit={handleEdit}
        onDelete={handleDelete}
        onTestConnection={handleTestConnection}
        getTestResult={connectionTestHook.getTestResult}
        testing={connectionTestHook.testing}
      />

      {/* Pagination - only show for server-side pagination (when not filtering) */}
      {!hasFilters && (
        <div className="mt-4">
          <ApplicationsPagination
            currentPage={applicationsHook.pagination.page}
            totalPages={applicationsHook.pagination.totalPages}
            pageSize={applicationsHook.pagination.pageSize}
            totalCount={applicationsHook.pagination.totalCount}
            onPageChange={applicationsHook.changePage}
            onPageSizeChange={applicationsHook.changePageSize}
            loading={applicationsHook.loading}
          />
        </div>
      )}

      {/* Application Form Modal */}
      <ApplicationForm
        show={showForm}
        onClose={handleFormClose}
        onSubmit={handleFormSubmit}
        formData={formHook.formData}
        errors={formHook.errors}
        isValid={formHook.isValid}
        isSubmitting={formHook.isSubmitting}
        isEditing={formHook.isEditing}
        showPasswords={formHook.showPasswords}
        onFieldChange={formHook.updateField}
        onTogglePasswordVisibility={formHook.togglePasswordVisibility}
        getAuthenticationOptions={formHook.getAuthenticationOptions}
      />

      {/* Delete Confirmation Modal */}
      <ApplicationDeleteModal
        show={showDeleteModal}
        application={applicationToDelete}
        onClose={handleDeleteModalClose}
        onConfirm={handleDeleteConfirm}
        isDeleting={applicationsHook.loading}
      />
    </div>
  );
};

export default ApplicationsDashboard;
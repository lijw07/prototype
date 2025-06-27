// ApplicationSearchFilters component - handles search and filtering UI
// Follows SRP by focusing only on filter interface logic

import React from 'react';
import { Search, Filter } from 'lucide-react';
import { DataSourceTypeEnum } from './hooks/useApplicationForm';

interface ApplicationSearchFiltersProps {
  searchTerm: string;
  connectionType: string;
  authType: string;
  sortOrder: 'newest' | 'oldest';
  onSearchChange: (searchTerm: string) => void;
  onConnectionTypeChange: (connectionType: string) => void;
  onAuthTypeChange: (authType: string) => void;
  onSortOrderChange: (sortOrder: 'newest' | 'oldest') => void;
  onClearFilters: () => void;
  totalCount: number;
  filteredCount: number;
}

export const ApplicationSearchFilters: React.FC<ApplicationSearchFiltersProps> = ({
  searchTerm,
  connectionType,
  authType,
  sortOrder,
  onSearchChange,
  onConnectionTypeChange,
  onAuthTypeChange,
  onSortOrderChange,
  onClearFilters,
  totalCount,
  filteredCount
}) => {
  const hasActiveFilters = searchTerm !== '' || connectionType !== 'all' || authType !== 'all' || sortOrder !== 'newest';

  // Get unique data source types for filter dropdown
  const getDataSourceOptions = () => {
    return Object.entries(DataSourceTypeEnum).map(([value, label]) => ({
      value,
      label
    }));
  };

  const authTypeOptions = [
    { value: 'all', label: 'All Authentication Types' },
    { value: 'UserPassword', label: 'Username/Password' },
    { value: 'Integrated', label: 'Integrated Security' },
    { value: 'AzureAdPassword', label: 'Azure AD Password' },
    { value: 'AzureAdIntegrated', label: 'Azure AD Integrated' },
    { value: 'NoAuth', label: 'No Authentication' }
  ];

  return (
    <div className="card border-0 shadow-sm mb-4">
      <div className="card-body">
        <div className="row g-3">
          {/* Search Input */}
          <div className="col-12 col-md-6 col-lg-4">
            <div className="position-relative">
              <Search 
                size={16} 
                className="position-absolute top-50 start-0 translate-middle-y ms-3 text-muted" 
              />
              <input
                type="text"
                className="form-control ps-5"
                placeholder="Search applications..."
                value={searchTerm}
                onChange={(e) => onSearchChange(e.target.value)}
              />
            </div>
          </div>

          {/* Connection Type Filter */}
          <div className="col-12 col-md-6 col-lg-2">
            <select
              className="form-select"
              value={connectionType}
              onChange={(e) => onConnectionTypeChange(e.target.value)}
            >
              <option value="all">All Types</option>
              {getDataSourceOptions().map(option => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>

          {/* Authentication Type Filter */}
          <div className="col-12 col-md-6 col-lg-2">
            <select
              className="form-select"
              value={authType}
              onChange={(e) => onAuthTypeChange(e.target.value)}
            >
              {authTypeOptions.map(option => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>

          {/* Sort Order */}
          <div className="col-12 col-md-6 col-lg-2">
            <select
              className="form-select"
              value={sortOrder}
              onChange={(e) => onSortOrderChange(e.target.value as 'newest' | 'oldest')}
            >
              <option value="newest">Newest First</option>
              <option value="oldest">Oldest First</option>
            </select>
          </div>

          {/* Clear Filters Button */}
          <div className="col-12 col-lg-2">
            <button
              className="btn btn-outline-secondary w-100"
              onClick={onClearFilters}
              disabled={!hasActiveFilters}
            >
              <Filter size={16} className="me-2" />
              Clear Filters
            </button>
          </div>
        </div>

        {/* Results Summary */}
        <div className="row mt-3">
          <div className="col-12">
            <div className="d-flex justify-content-between align-items-center">
              <div className="text-muted small">
                {hasActiveFilters ? (
                  <>
                    Showing {filteredCount} of {totalCount} applications
                    {searchTerm && (
                      <span className="ms-2">
                        matching "<strong>{searchTerm}</strong>"
                      </span>
                    )}
                  </>
                ) : (
                  `Showing all ${totalCount} applications`
                )}
              </div>

              {hasActiveFilters && (
                <div className="d-flex gap-2">
                  {searchTerm && (
                    <span className="badge bg-primary">
                      Search: {searchTerm}
                    </span>
                  )}
                  {connectionType !== 'all' && (
                    <span className="badge bg-info">
                      Type: {DataSourceTypeEnum[parseInt(connectionType)] || connectionType}
                    </span>
                  )}
                  {authType !== 'all' && (
                    <span className="badge bg-warning">
                      Auth: {authType}
                    </span>
                  )}
                  {sortOrder !== 'newest' && (
                    <span className="badge bg-secondary">
                      Sort: {sortOrder}
                    </span>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ApplicationSearchFilters;
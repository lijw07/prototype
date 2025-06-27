// ApplicationsPagination component - handles pagination controls
// Follows SRP by focusing only on pagination UI logic

import React from 'react';
import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-react';

interface ApplicationsPaginationProps {
  currentPage: number;
  totalPages: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
  loading?: boolean;
}

export const ApplicationsPagination: React.FC<ApplicationsPaginationProps> = ({
  currentPage,
  totalPages,
  pageSize,
  totalCount,
  onPageChange,
  onPageSizeChange,
  loading = false
}) => {
  // Calculate visible page range
  const getVisiblePages = () => {
    const delta = 2; // Show 2 pages before and after current page
    const range: number[] = [];
    const rangeWithDots: (number | string)[] = [];

    for (
      let i = Math.max(2, currentPage - delta);
      i <= Math.min(totalPages - 1, currentPage + delta);
      i++
    ) {
      range.push(i);
    }

    if (currentPage - delta > 2) {
      rangeWithDots.push(1, '...');
    } else {
      rangeWithDots.push(1);
    }

    rangeWithDots.push(...range);

    if (currentPage + delta < totalPages - 1) {
      rangeWithDots.push('...', totalPages);
    } else {
      if (totalPages > 1) {
        rangeWithDots.push(totalPages);
      }
    }

    return rangeWithDots;
  };

  // Calculate display range
  const startIndex = (currentPage - 1) * pageSize + 1;
  const endIndex = Math.min(currentPage * pageSize, totalCount);

  if (totalPages <= 1) {
    return null;
  }

  const visiblePages = getVisiblePages();

  return (
    <div className="card border-0 shadow-sm">
      <div className="card-body">
        <div className="row align-items-center">
          {/* Results Info */}
          <div className="col-12 col-md-6 mb-3 mb-md-0">
            <div className="d-flex align-items-center gap-3">
              <span className="text-muted">
                Showing {startIndex}-{endIndex} of {totalCount} applications
              </span>
              
              {/* Page Size Selector */}
              <div className="d-flex align-items-center gap-2">
                <label className="form-label mb-0 text-muted small">Show:</label>
                <select
                  className="form-select form-select-sm"
                  style={{ width: 'auto' }}
                  value={pageSize}
                  onChange={(e) => onPageSizeChange(parseInt(e.target.value))}
                  disabled={loading}
                >
                  <option value={4}>4</option>
                  <option value={8}>8</option>
                  <option value={12}>12</option>
                  <option value={20}>20</option>
                  <option value={50}>50</option>
                </select>
              </div>
            </div>
          </div>

          {/* Pagination Controls */}
          <div className="col-12 col-md-6">
            <nav aria-label="Applications pagination">
              <ul className="pagination pagination-sm justify-content-md-end justify-content-center mb-0">
                {/* First Page */}
                <li className={`page-item ${currentPage === 1 ? 'disabled' : ''}`}>
                  <button
                    className="page-link"
                    onClick={() => onPageChange(1)}
                    disabled={currentPage === 1 || loading}
                    aria-label="First page"
                  >
                    <ChevronsLeft size={16} />
                  </button>
                </li>

                {/* Previous Page */}
                <li className={`page-item ${currentPage === 1 ? 'disabled' : ''}`}>
                  <button
                    className="page-link"
                    onClick={() => onPageChange(currentPage - 1)}
                    disabled={currentPage === 1 || loading}
                    aria-label="Previous page"
                  >
                    <ChevronLeft size={16} />
                  </button>
                </li>

                {/* Page Numbers */}
                {visiblePages.map((page, index) => (
                  <li key={index} className={`page-item ${page === currentPage ? 'active' : ''} ${typeof page === 'string' ? 'disabled' : ''}`}>
                    {typeof page === 'string' ? (
                      <span className="page-link">...</span>
                    ) : (
                      <button
                        className="page-link"
                        onClick={() => onPageChange(page)}
                        disabled={loading}
                        aria-label={`Page ${page}`}
                        aria-current={page === currentPage ? 'page' : undefined}
                      >
                        {page}
                      </button>
                    )}
                  </li>
                ))}

                {/* Next Page */}
                <li className={`page-item ${currentPage === totalPages ? 'disabled' : ''}`}>
                  <button
                    className="page-link"
                    onClick={() => onPageChange(currentPage + 1)}
                    disabled={currentPage === totalPages || loading}
                    aria-label="Next page"
                  >
                    <ChevronRight size={16} />
                  </button>
                </li>

                {/* Last Page */}
                <li className={`page-item ${currentPage === totalPages ? 'disabled' : ''}`}>
                  <button
                    className="page-link"
                    onClick={() => onPageChange(totalPages)}
                    disabled={currentPage === totalPages || loading}
                    aria-label="Last page"
                  >
                    <ChevronsRight size={16} />
                  </button>
                </li>
              </ul>
            </nav>
          </div>
        </div>

        {/* Mobile-friendly pagination info */}
        <div className="d-md-none mt-3 text-center">
          <small className="text-muted">
            Page {currentPage} of {totalPages}
          </small>
        </div>
      </div>
    </div>
  );
};

export default ApplicationsPagination;
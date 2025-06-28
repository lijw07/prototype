import React from 'react';
import LoadingSpinner from './LoadingSpinner';
import Pagination from './Pagination';
import { ArrowUp, ArrowDown } from 'lucide-react';

export interface Column<T> {
  key: string;
  header: string;
  render?: (item: T) => React.ReactNode;
  sortable?: boolean;
  className?: string;
  headerClassName?: string;
}

interface DataTableProps<T> {
  data: T[];
  columns: Column<T>[];
  loading?: boolean;
  pagination?: {
    currentPage: number;
    totalPages: number;
    pageSize: number;
    totalCount: number;
    onPageChange: (page: number) => void;
    onPageSizeChange?: (size: number) => void;
  };
  onSort?: (key: string, direction: 'asc' | 'desc') => void;
  sortKey?: string;
  sortDirection?: 'asc' | 'desc';
  emptyMessage?: string;
  className?: string;
  striped?: boolean;
  hover?: boolean;
  bordered?: boolean;
  size?: 'sm' | 'md' | 'lg';
}

export default function DataTable<T extends Record<string, any>>({
  data,
  columns,
  loading = false,
  pagination,
  onSort,
  sortKey,
  sortDirection,
  emptyMessage = 'No data available',
  className = '',
  striped = true,
  hover = true,
  bordered = false,
  size = 'md'
}: DataTableProps<T>) {
  const handleSort = (key: string) => {
    if (!onSort) return;
    
    const newDirection = sortKey === key && sortDirection === 'asc' ? 'desc' : 'asc';
    onSort(key, newDirection);
  };

  const renderSortIcon = (columnKey: string) => {
    if (sortKey !== columnKey) return null;
    
    return sortDirection === 'asc' ? 
      <ArrowUp size={14} className="ms-1" /> : 
      <ArrowDown size={14} className="ms-1" />;
  };

  const tableClasses = [
    'table',
    'table-responsive',
    striped && 'table-striped',
    hover && 'table-hover',
    bordered && 'table-bordered',
    size === 'sm' && 'table-sm',
    size === 'lg' && 'table-lg',
    className
  ].filter(Boolean).join(' ');

  if (loading) {
    return (
      <div className="card">
        <div className="card-body py-5">
          <LoadingSpinner text="Loading data..." />
        </div>
      </div>
    );
  }

  return (
    <div className="data-table-container">
      <div className="table-responsive">
        <table className={tableClasses}>
          <thead>
            <tr>
              {columns.map((column) => (
                <th 
                  key={column.key}
                  className={`${column.headerClassName || ''} ${column.sortable && onSort ? 'cursor-pointer user-select-none' : ''}`}
                  onClick={() => column.sortable && handleSort(column.key)}
                >
                  <div className="d-flex align-items-center">
                    {column.header}
                    {column.sortable && renderSortIcon(column.key)}
                  </div>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {data.length === 0 ? (
              <tr>
                <td colSpan={columns.length} className="text-center py-4 text-muted">
                  {emptyMessage}
                </td>
              </tr>
            ) : (
              data.map((item, index) => (
                <tr key={item.id || index}>
                  {columns.map((column) => (
                    <td key={column.key} className={column.className}>
                      {column.render ? 
                        column.render(item) : 
                        item[column.key]?.toString() || '-'
                      }
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
      
      {pagination && (
        <div className="mt-3">
          <Pagination {...pagination} />
        </div>
      )}
    </div>
  );
}
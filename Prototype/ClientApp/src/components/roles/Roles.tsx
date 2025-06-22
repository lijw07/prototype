import React, { useState, useEffect } from 'react';
import { Shield, Plus, Edit, Trash2, Users, Key, CheckCircle2, AlertCircle, Loader, AlertTriangle, ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight, Search } from 'lucide-react';
import { roleApi } from '../../services/api';

interface Role {
    userRoleId: string;
    role: string;
    createdAt: string;
    createdBy: string;
}

const Roles: React.FC = () => {
    const [roles, setRoles] = useState<Role[]>([]);
    const [allRoles, setAllRoles] = useState<Role[]>([]); // Store all roles for client-side operations
    const [loading, setLoading] = useState(false);
    const [showRoleForm, setShowRoleForm] = useState(false);
    const [editingRole, setEditingRole] = useState<Role | null>(null);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [submitSuccess, setSubmitSuccess] = useState(false);
    const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
    const [deletingRole, setDeletingRole] = useState<Role | null>(null);
    const [deleteSuccess, setDeleteSuccess] = useState(false);

    // Pagination state
    const [currentPage, setCurrentPage] = useState(1);
    const [pageSize, setPageSize] = useState(4);
    const [totalCount, setTotalCount] = useState(0);
    const [totalPages, setTotalPages] = useState(0);

    // Search and sorting state
    const [searchTerm, setSearchTerm] = useState('');
    const [sortOrder, setSortOrder] = useState<'newest' | 'oldest'>('newest');

    const [roleForm, setRoleForm] = useState({
        roleName: ''
    });

    const fetchAllRoles = async () => {
        try {
            // Fetch all roles by requesting a large page size
            const response = await roleApi.getAllRoles(1, 1000); // Large enough to get all
            if (response.success && response.data?.data) {
                setAllRoles(response.data.data);
                return response.data.data;
            } else if (response.success && response.roles) {
                setAllRoles(response.roles);
                return response.roles;
            }
            return [];
        } catch (error) {
            console.error('Failed to fetch all roles:', error);
            return [];
        }
    };

    const fetchRoles = async (page: number = currentPage, size: number = pageSize) => {
        setLoading(true);
        try {
            console.log('Fetching roles...');
            const response = await roleApi.getAllRoles(page, size);
            console.log('Fetch roles response:', response);
            if (response.success && response.data?.data) {
                console.log('Setting roles:', response.data.data);
                setRoles(response.data.data);
                setCurrentPage(response.data.page || page);
                setPageSize(response.data.pageSize || size);
                setTotalCount(response.data.totalCount || 0);
                setTotalPages(response.data.totalPages || 1);
            } else if (response.success && response.roles) {
                // Fallback for old API response format - use client-side pagination
                console.log('Setting roles (fallback):', response.roles);
                const startIndex = (page - 1) * size;
                const endIndex = startIndex + size;
                const paginatedRoles = response.roles.slice(startIndex, endIndex);
                setRoles(paginatedRoles);
                // Set pagination values for non-paginated response
                setTotalCount(response.roles.length);
                setTotalPages(Math.ceil(response.roles.length / size));
                setCurrentPage(page);
                setPageSize(size);
            } else {
                console.log('No roles returned or response not successful');
            }
        } catch (error) {
            console.error('Failed to fetch roles:', error);
        } finally {
            setLoading(false);
        }
    };

    // Smart refetch that handles empty pages after deletion
    const refetchRoles = async () => {
        // First, check the current state after deletion
        const response = await roleApi.getAllRoles(currentPage, pageSize);
        if (response.success) {
            let totalItems = 0;
            if (response.data?.totalCount !== undefined) {
                totalItems = response.data.totalCount;
            } else if (response.roles) {
                totalItems = response.roles.length;
            }
            
            const newTotalPages = Math.ceil(totalItems / pageSize);
            
            // Check if current page is beyond available pages
            if (currentPage > newTotalPages && totalItems > 0) {
                // Navigate to last available page
                setCurrentPage(newTotalPages);
                await fetchRoles(newTotalPages, pageSize);
            } else if (totalItems === 0) {
                // If no roles at all, go to page 1
                setCurrentPage(1);
                await fetchRoles(1, pageSize);
            } else {
                // Current page is valid, just refresh
                await fetchRoles(currentPage, pageSize);
            }
        } else {
            // If the request failed, just refresh current page
            await fetchRoles(currentPage, pageSize);
        }
        // Also refresh all roles
        await fetchAllRoles();
    };

    // Refetch and navigate to first page (where new items should appear)
    const refetchAndGoToFirstPage = async () => {
        setCurrentPage(1);
        await fetchRoles(1, pageSize);
        // Also refresh all roles
        await fetchAllRoles();
    };

    useEffect(() => {
        fetchRoles(1, pageSize);
        // Also fetch all roles for client-side operations
        fetchAllRoles();
    }, []);

    // Add escape key listener to close modals
    useEffect(() => {
        const handleEscapeKey = (event: KeyboardEvent) => {
            if (event.key === 'Escape') {
                if (showRoleForm) {
                    setShowRoleForm(false);
                    setEditingRole(null);
                    setRoleForm({ roleName: '' });
                }
            }
        };

        document.addEventListener('keydown', handleEscapeKey);
        return () => {
            document.removeEventListener('keydown', handleEscapeKey);
        };
    }, [showRoleForm]);

    const handleRoleSubmit = async () => {
        try {
            setIsSubmitting(true);
            if (editingRole) {
                // Update existing role
                const response = await roleApi.updateRole(editingRole.userRoleId, { roleName: roleForm.roleName });
                if (response.success) {
                    setSubmitSuccess(true);
                    refetchRoles(); // Stay on current page when editing
                    
                    // Role form will be closed manually by user clicking X
                } else {
                    alert(response.message || 'Failed to update role');
                }
            } else {
                // Create new role
                console.log('Creating role with name:', roleForm.roleName);
                const response = await roleApi.createRole({ roleName: roleForm.roleName });
                console.log('Create role response:', response);
                if (response.success) {
                    setSubmitSuccess(true);
                    console.log('Fetching roles after creation...');
                    refetchAndGoToFirstPage(); // Go to first page where new role should appear
                    
                    // Role form will be closed manually by user clicking X
                } else {
                    alert(response.message || 'Failed to create role');
                }
            }
        } catch (error: any) {
            console.error('Failed to save role:', error);
            alert(error.message || 'Failed to save role');
        } finally {
            setIsSubmitting(false);
        }
    };

    const deleteRole = async () => {
        if (!deletingRole) return;

        try {
            const response = await roleApi.deleteRole(deletingRole.userRoleId);
            if (response.success) {
                setDeleteSuccess(true);
                refetchRoles();
                
                // Delete success modal will be closed manually by user clicking X
            } else {
                alert(response.message || 'Failed to delete role');
            }
        } catch (error: any) {
            console.error('Failed to delete role:', error);
            alert(error.message || 'Failed to delete role');
        }
    };

    const confirmDeleteRole = (role: Role) => {
        setDeletingRole(role);
        setShowDeleteConfirm(true);
    };


    const openEditRole = (role: Role) => {
        setEditingRole(role);
        setRoleForm({
            roleName: role.role
        });
        setShowRoleForm(true);
    };

    // Use filtered/sorted roles for display (when filters are active)
    // Use backend pagination when no filters are applied
    const hasFilters = searchTerm !== '' || sortOrder === 'oldest';
    
    // Use allRoles for filtering/sorting, roles for backend pagination
    const sourceRoles = hasFilters ? allRoles : roles;
    
    // Filter and sort roles
    const filteredRoles = sourceRoles.filter(role => {
        const matchesSearch = searchTerm === '' || 
                             role.role.toLowerCase().includes(searchTerm.toLowerCase()) ||
                             role.createdBy.toLowerCase().includes(searchTerm.toLowerCase());
        return matchesSearch;
    });

    // Sort roles based on creation date
    const sortedRoles = [...filteredRoles].sort((a, b) => {
        const dateA = new Date(a.createdAt).getTime();
        const dateB = new Date(b.createdAt).getTime();
        return sortOrder === 'newest' ? dateB - dateA : dateA - dateB;
    });
    
    let currentRoles: Role[], displayTotalPages: number, displayTotalCount: number;
    if (hasFilters) {
        // Client-side pagination for filtered results
        const startIndex = (currentPage - 1) * pageSize;
        const endIndex = startIndex + pageSize;
        currentRoles = sortedRoles.slice(startIndex, endIndex);
        displayTotalPages = Math.ceil(sortedRoles.length / pageSize);
        displayTotalCount = sortedRoles.length;
    } else {
        // Backend pagination for unfiltered results
        currentRoles = roles;
        displayTotalPages = totalPages;
        displayTotalCount = totalCount;
    }

    return (
        <div className="min-vh-100 bg-light" style={{overflowX: 'hidden'}}>
            <div className="container-fluid py-4" style={{maxWidth: '100%'}}>
                {/* Header */}
                <div className="mb-4">
                    <div className="d-flex align-items-center mb-2">
                        <Shield className="text-primary me-3" size={32} />
                        <h1 className="display-5 fw-bold text-dark mb-0">Roles</h1>
                    </div>
                    <p className="text-muted fs-6">Manage user roles and their permissions</p>
                </div>

                {/* Roles */}
                <div className="card shadow-sm border-0 rounded-4">
                    <div className="card-body p-4">
                        <div className="d-flex justify-content-between align-items-center mb-4">
                            <h2 className="card-title fw-bold text-dark mb-0 d-flex align-items-center">
                                <Key className="text-primary me-2" size={24} />
                                System Roles
                            </h2>
                            <button
                                onClick={() => setShowRoleForm(true)}
                                className="btn btn-primary rounded-3 fw-semibold d-flex align-items-center"
                            >
                                <Plus className="me-2" size={18} />
                                <span>Create Role</span>
                            </button>
                        </div>

                        {/* Search and Sort Controls */}
                        <div className="row g-3 mb-4">
                            <div className="col-md-8">
                                <div className="position-relative">
                                    <Search className="position-absolute top-50 start-0 translate-middle-y ms-3 text-muted" size={16} />
                                    <input
                                        type="text"
                                        className="form-control rounded-3 ps-5"
                                        placeholder="Search roles..."
                                        value={searchTerm}
                                        onChange={(e) => {
                                            setSearchTerm(e.target.value);
                                            setCurrentPage(1); // Reset to first page when searching
                                        }}
                                    />
                                </div>
                            </div>
                            <div className="col-md-4">
                                <select
                                    className="form-select rounded-3"
                                    value={sortOrder}
                                    onChange={async (e) => {
                                        const newSortOrder = e.target.value as 'newest' | 'oldest';
                                        setSortOrder(newSortOrder);
                                        setCurrentPage(1); // Reset to first page when sorting changes
                                        
                                        // If switching to oldest first and we don't have all roles, fetch them
                                        if (newSortOrder === 'oldest' && allRoles.length === 0) {
                                            await fetchAllRoles();
                                        }
                                    }}
                                >
                                    <option value="newest">Newest First</option>
                                    <option value="oldest">Oldest First</option>
                                </select>
                            </div>
                        </div>

                        {loading ? (
                            <div className="d-flex align-items-center text-muted">
                                <div className="spinner-border spinner-border-sm me-2" role="status">
                                    <span className="visually-hidden">Loading...</span>
                                </div>
                                Loading roles...
                            </div>
                        ) : (
                            <>
                                <div className="row g-3">
                                    {currentRoles.map((role) => (
                                        <div key={role.userRoleId} className="col-12">
                                            <div 
                                                className="card border border-light rounded-3 h-100" 
                                                style={{cursor: 'pointer'}}
                                                onClick={() => openEditRole(role)}
                                            >
                                                <div className="card-body p-4">
                                                    <div className="d-flex justify-content-between align-items-start mb-3">
                                                        <div className="flex-grow-1">
                                                            <h5 className="card-title fw-bold text-dark mb-1 d-flex align-items-center" style={{wordWrap: 'break-word', overflowWrap: 'break-word'}}>
                                                                {role.role}
                                                                <CheckCircle2 className="text-success ms-2" size={16} />
                                                            </h5>
                                                            <p className="card-text text-muted small mb-2">
                                                                Created by {role.createdBy} on {new Date(role.createdAt).toLocaleDateString()}
                                                            </p>
                                                        </div>
                                                        <div className="d-flex flex-column gap-1">
                                                            <button
                                                                onClick={(e) => {
                                                                    e.stopPropagation();
                                                                    confirmDeleteRole(role);
                                                                }}
                                                                className="btn btn-outline-danger btn-sm rounded-3"
                                                                title="Delete Role"
                                                            >
                                                                <Trash2 size={14} />
                                                            </button>
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    ))}
                                    {currentRoles.length === 0 && !loading && (
                                        <div className="col-12">
                                            <div className="text-center py-5 text-muted">
                                                <Shield size={48} className="mb-3 opacity-50" />
                                                <p>{hasFilters ? 'No roles match your search' : 'No roles configured yet'}</p>
                                            </div>
                                        </div>
                                    )}
                                </div>

                                {/* Pagination */}
                                {displayTotalCount > 4 && (
                            <div className="d-flex justify-content-between align-items-center mt-4">
                                <div className="d-flex align-items-center gap-3">
                                    <span className="text-muted">
                                        {hasFilters ? (
                                            `Showing ${currentRoles.length} of ${displayTotalCount} roles (filtered)`
                                        ) : (
                                            `Showing ${((currentPage - 1) * pageSize) + 1} to ${Math.min(currentPage * pageSize, displayTotalCount)} of ${displayTotalCount} roles`
                                        )}
                                    </span>
                                    <div className="d-flex align-items-center gap-2">
                                        <span className="text-muted small">Roles per page:</span>
                                        <select 
                                            className="form-select form-select-sm" 
                                            style={{width: 'auto'}}
                                            value={pageSize}
                                            onChange={(e) => {
                                                const newPageSize = Number(e.target.value);
                                                setPageSize(newPageSize);
                                                setCurrentPage(1);
                                                if (!hasFilters) {
                                                    fetchRoles(1, newPageSize);
                                                }
                                            }}
                                        >
                                            <option value={4}>4</option>
                                            <option value={10}>10</option>
                                            <option value={20}>20</option>
                                            <option value={50}>50</option>
                                        </select>
                                    </div>
                                </div>
                                
                                <nav>
                                    <ul className="pagination pagination-sm mb-0">
                                        <li className={`page-item ${currentPage === 1 ? 'disabled' : ''}`}>
                                            <button 
                                                className="page-link" 
                                                onClick={() => {
                                                    setCurrentPage(1);
                                                    if (!hasFilters) {
                                                        fetchRoles(1, pageSize);
                                                    }
                                                }}
                                                disabled={currentPage === 1}
                                            >
                                                <ChevronsLeft size={16} />
                                            </button>
                                        </li>
                                        <li className={`page-item ${currentPage === 1 ? 'disabled' : ''}`}>
                                            <button 
                                                className="page-link" 
                                                onClick={() => {
                                                    const newPage = currentPage - 1;
                                                    setCurrentPage(newPage);
                                                    if (!hasFilters) {
                                                        fetchRoles(newPage, pageSize);
                                                    }
                                                }}
                                                disabled={currentPage === 1}
                                            >
                                                <ChevronLeft size={16} />
                                            </button>
                                        </li>
                                        
                                        {/* Page numbers */}
                                        {Array.from({ length: Math.min(5, displayTotalPages) }, (_, i) => {
                                            let pageNum: number;
                                            if (displayTotalPages <= 5) {
                                                pageNum = i + 1;
                                            } else if (currentPage <= 3) {
                                                pageNum = i + 1;
                                            } else if (currentPage >= displayTotalPages - 2) {
                                                pageNum = displayTotalPages - 4 + i;
                                            } else {
                                                pageNum = currentPage - 2 + i;
                                            }
                                            
                                            return (
                                                <li key={pageNum} className={`page-item ${currentPage === pageNum ? 'active' : ''}`}>
                                                    <button 
                                                        className="page-link" 
                                                        onClick={() => {
                                                            setCurrentPage(pageNum);
                                                            if (!hasFilters) {
                                                                fetchRoles(pageNum, pageSize);
                                                            }
                                                        }}
                                                    >
                                                        {pageNum}
                                                    </button>
                                                </li>
                                            );
                                        })}
                                        
                                        <li className={`page-item ${currentPage === displayTotalPages ? 'disabled' : ''}`}>
                                            <button 
                                                className="page-link" 
                                                onClick={() => {
                                                    const newPage = currentPage + 1;
                                                    setCurrentPage(newPage);
                                                    if (!hasFilters) {
                                                        fetchRoles(newPage, pageSize);
                                                    }
                                                }}
                                                disabled={currentPage === displayTotalPages}
                                            >
                                                <ChevronRight size={16} />
                                            </button>
                                        </li>
                                        <li className={`page-item ${currentPage === displayTotalPages ? 'disabled' : ''}`}>
                                            <button 
                                                className="page-link" 
                                                onClick={() => {
                                                    setCurrentPage(displayTotalPages);
                                                    if (!hasFilters) {
                                                        fetchRoles(displayTotalPages, pageSize);
                                                    }
                                                }}
                                                disabled={currentPage === displayTotalPages}
                                            >
                                                <ChevronsRight size={16} />
                                            </button>
                                        </li>
                                    </ul>
                                </nav>
                            </div>
                        )}
                            </>
                        )}
                    </div>
                </div>

                {/* Role Form Modal */}
                {showRoleForm && (
                    <div className="modal d-block" style={{backgroundColor: 'rgba(0,0,0,0.5)', position: 'fixed', top: 0, left: 0, width: '100%', height: '100%', zIndex: 1050}}>
                        <div className="modal-dialog modal-dialog-centered" style={{maxWidth: '90%', margin: '1.75rem auto'}}>
                            <div className="modal-content border-0 rounded-4">
                                <div className="modal-header border-0 pb-0">
                                    <h3 className="modal-title fw-bold">
                                        {editingRole ? 'Edit Role' : 'Create New Role'}
                                    </h3>
                                    <button
                                        type="button"
                                        className="btn-close"
                                        onClick={() => {
                                            setShowRoleForm(false);
                                            setEditingRole(null);
                                            setSubmitSuccess(false);
                                            setRoleForm({
                                                roleName: ''
                                            });
                                        }}
                                    ></button>
                                </div>
                                <div className="modal-body">
                                    {submitSuccess ? (
                                        <div className="text-center py-4">
                                            <CheckCircle2 size={64} className="text-success mb-3" />
                                            <h4 className="text-success fw-bold">
                                                {editingRole ? 'Role Updated Successfully!' : 'Role Created Successfully!'}
                                            </h4>
                                            <p className="text-muted">
                                                {editingRole ? 'The role has been updated.' : 'The new role has been created.'}
                                            </p>
                                        </div>
                                    ) : (
                                        <div className="row g-4">
                                            <div className="col-12">
                                                <h5 className="fw-bold mb-3">Role Information</h5>
                                                <div className="mb-3">
                                                    <label className="form-label fw-semibold">Role Name</label>
                                                    <input
                                                        type="text"
                                                        value={roleForm.roleName}
                                                        onChange={(e) => setRoleForm({...roleForm, roleName: e.target.value})}
                                                        className="form-control rounded-3"
                                                        placeholder="Enter role name"
                                                        disabled={isSubmitting}
                                                    />
                                                </div>
                                            </div>
                                        </div>
                                    )}
                                </div>
                                <div className="modal-footer border-0 pt-0">
                                    {!submitSuccess && (
                                        <button
                                            onClick={handleRoleSubmit}
                                            className="btn btn-primary rounded-3 fw-semibold d-flex align-items-center"
                                            disabled={!roleForm.roleName.trim() || isSubmitting}
                                        >
                                            {isSubmitting ? (
                                                <>
                                                    <Loader className="me-2 animate-spin" size={16} />
                                                    {editingRole ? 'Updating...' : 'Creating...'}
                                                </>
                                            ) : (
                                                editingRole ? 'Update Role' : 'Create Role'
                                            )}
                                        </button>
                                    )}
                                </div>
                            </div>
                        </div>
                    </div>
                )}

                {/* Delete Confirmation Modal */}
                {showDeleteConfirm && deletingRole && (
                    <div className="modal d-block" style={{backgroundColor: 'rgba(0,0,0,0.5)', position: 'fixed', top: 0, left: 0, width: '100%', height: '100%', zIndex: 1050}}>
                        <div className="modal-dialog modal-dialog-centered">
                            <div className="modal-content border-0 rounded-4">
                                <div className="modal-header border-0 pb-0">
                                    <h3 className="modal-title fw-bold text-danger">
                                        {deleteSuccess ? 'Role Deleted' : 'Confirm Deletion'}
                                    </h3>
                                    <button
                                        type="button"
                                        className="btn-close"
                                        onClick={() => {
                                            setShowDeleteConfirm(false);
                                            setDeletingRole(null);
                                            setDeleteSuccess(false);
                                        }}
                                    ></button>
                                </div>
                                <div className="modal-body">
                                    {deleteSuccess ? (
                                        <div className="text-center py-4">
                                            <CheckCircle2 size={64} className="text-success mb-3" />
                                            <h4 className="text-success fw-bold">Role Deleted Successfully!</h4>
                                            <p className="text-muted">
                                                <strong>{deletingRole.role}</strong> has been permanently removed.
                                            </p>
                                        </div>
                                    ) : (
                                        <div className="text-center py-4">
                                            <AlertTriangle size={64} className="text-warning mb-3" />
                                            <h4 className="fw-bold">Are you sure you want to delete this role?</h4>
                                            <div className="bg-light rounded-3 p-3 my-3">
                                                <h5 className="fw-bold text-dark mb-1">{deletingRole.role}</h5>
                                                <p className="text-muted small mb-0">Created by {deletingRole.createdBy} on {new Date(deletingRole.createdAt).toLocaleDateString()}</p>
                                            </div>
                                            <p className="text-danger small fw-semibold">
                                                ⚠️ This action cannot be undone. Users with this role may lose access permissions.
                                            </p>
                                        </div>
                                    )}
                                </div>
                                {!deleteSuccess && (
                                    <div className="modal-footer border-0 pt-0">
                                        <button
                                            onClick={() => {
                                                setShowDeleteConfirm(false);
                                                setDeletingRole(null);
                                            }}
                                            className="btn btn-secondary rounded-3 fw-semibold"
                                        >
                                            Cancel
                                        </button>
                                        <button
                                            onClick={deleteRole}
                                            className="btn btn-danger rounded-3 fw-semibold d-flex align-items-center"
                                        >
                                            <Trash2 className="me-2" size={16} />
                                            Delete Role
                                        </button>
                                    </div>
                                )}
                            </div>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};

export default Roles;
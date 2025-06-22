import React, { useState, useEffect } from 'react';
import { Shield, Plus, Edit, Trash2, Users, Key, CheckCircle2, AlertCircle, Loader, AlertTriangle, ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-react';
import { roleApi } from '../../services/api';

interface Role {
    userRoleId: string;
    role: string;
    createdAt: string;
    createdBy: string;
}

const Roles: React.FC = () => {
    const [roles, setRoles] = useState<Role[]>([]);
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
    const [rolesPerPage, setRolesPerPage] = useState(20);

    const [roleForm, setRoleForm] = useState({
        roleName: ''
    });

    const fetchRoles = async () => {
        setLoading(true);
        try {
            console.log('Fetching roles...');
            const response = await roleApi.getAllRoles();
            console.log('Fetch roles response:', response);
            if (response.success && response.roles) {
                console.log('Setting roles:', response.roles);
                setRoles(response.roles);
            } else {
                console.log('No roles returned or response not successful');
            }
        } catch (error) {
            console.error('Failed to fetch roles:', error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchRoles();
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
                    fetchRoles();
                    
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
                    fetchRoles();
                    
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
                fetchRoles();
                
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

                        {loading ? (
                            <div className="d-flex align-items-center text-muted">
                                <div className="spinner-border spinner-border-sm me-2" role="status">
                                    <span className="visually-hidden">Loading...</span>
                                </div>
                                Loading roles...
                            </div>
                        ) : (
                            <div className="row g-3">
                                {roles
                                    .slice((currentPage - 1) * rolesPerPage, currentPage * rolesPerPage)
                                    .map((role) => (
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
                                {roles.length === 0 && (
                                    <div className="col-12">
                                        <div className="text-center py-5 text-muted">
                                            <Shield size={48} className="mb-3 opacity-50" />
                                            <p>No roles configured yet</p>
                                        </div>
                                    </div>
                                )}
                            </div>
                        )}

                        {/* Pagination */}
                        {roles.length > 0 && Math.ceil(roles.length / rolesPerPage) > 1 && (
                            <div className="d-flex justify-content-between align-items-center mt-4">
                                <div className="d-flex align-items-center gap-3">
                                    <span className="text-muted">
                                        Showing {((currentPage - 1) * rolesPerPage) + 1} to {Math.min(currentPage * rolesPerPage, roles.length)} of {roles.length} roles
                                    </span>
                                    <div className="d-flex align-items-center gap-2">
                                        <span className="text-muted small">Roles per page:</span>
                                        <select 
                                            className="form-select form-select-sm" 
                                            style={{width: 'auto'}}
                                            value={rolesPerPage}
                                            onChange={(e) => {
                                                setRolesPerPage(Number(e.target.value));
                                                setCurrentPage(1);
                                            }}
                                        >
                                            <option value={10}>10</option>
                                            <option value={20}>20</option>
                                            <option value={50}>50</option>
                                            <option value={100}>100</option>
                                        </select>
                                    </div>
                                </div>
                                
                                <nav>
                                    <ul className="pagination pagination-sm mb-0">
                                        <li className={`page-item ${currentPage === 1 ? 'disabled' : ''}`}>
                                            <button 
                                                className="page-link" 
                                                onClick={() => setCurrentPage(1)}
                                                disabled={currentPage === 1}
                                            >
                                                <ChevronsLeft size={16} />
                                            </button>
                                        </li>
                                        <li className={`page-item ${currentPage === 1 ? 'disabled' : ''}`}>
                                            <button 
                                                className="page-link" 
                                                onClick={() => setCurrentPage(currentPage - 1)}
                                                disabled={currentPage === 1}
                                            >
                                                <ChevronLeft size={16} />
                                            </button>
                                        </li>
                                        
                                        {/* Page numbers */}
                                        {Array.from({ length: Math.min(5, Math.ceil(roles.length / rolesPerPage)) }, (_, i) => {
                                            const totalPages = Math.ceil(roles.length / rolesPerPage);
                                            let pageNum: number;
                                            if (totalPages <= 5) {
                                                pageNum = i + 1;
                                            } else if (currentPage <= 3) {
                                                pageNum = i + 1;
                                            } else if (currentPage >= totalPages - 2) {
                                                pageNum = totalPages - 4 + i;
                                            } else {
                                                pageNum = currentPage - 2 + i;
                                            }
                                            
                                            return (
                                                <li key={pageNum} className={`page-item ${currentPage === pageNum ? 'active' : ''}`}>
                                                    <button 
                                                        className="page-link" 
                                                        onClick={() => setCurrentPage(pageNum)}
                                                    >
                                                        {pageNum}
                                                    </button>
                                                </li>
                                            );
                                        })}
                                        
                                        <li className={`page-item ${currentPage === Math.ceil(roles.length / rolesPerPage) ? 'disabled' : ''}`}>
                                            <button 
                                                className="page-link" 
                                                onClick={() => setCurrentPage(currentPage + 1)}
                                                disabled={currentPage === Math.ceil(roles.length / rolesPerPage)}
                                            >
                                                <ChevronRight size={16} />
                                            </button>
                                        </li>
                                        <li className={`page-item ${currentPage === Math.ceil(roles.length / rolesPerPage) ? 'disabled' : ''}`}>
                                            <button 
                                                className="page-link" 
                                                onClick={() => setCurrentPage(Math.ceil(roles.length / rolesPerPage))}
                                                disabled={currentPage === Math.ceil(roles.length / rolesPerPage)}
                                            >
                                                <ChevronsRight size={16} />
                                            </button>
                                        </li>
                                    </ul>
                                </nav>
                            </div>
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
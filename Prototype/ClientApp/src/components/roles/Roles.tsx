import React, { useState, useEffect } from 'react';
import { Shield, Plus, Edit, Trash2, Users, Key, CheckCircle2, AlertCircle } from 'lucide-react';
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

    const [roleForm, setRoleForm] = useState({
        roleName: ''
    });

    const fetchRoles = async () => {
        setLoading(true);
        try {
            const response = await roleApi.getAllRoles();
            if (response.success && response.roles) {
                setRoles(response.roles);
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

    const handleRoleSubmit = async () => {
        try {
            if (editingRole) {
                // Update existing role
                const response = await roleApi.updateRole(editingRole.userRoleId, { roleName: roleForm.roleName });
                if (response.success) {
                    alert('Role updated successfully!');
                    fetchRoles();
                } else {
                    alert(response.message || 'Failed to update role');
                }
            } else {
                // Create new role
                const response = await roleApi.createRole({ roleName: roleForm.roleName });
                if (response.success) {
                    alert('Role created successfully!');
                    fetchRoles();
                } else {
                    alert(response.message || 'Failed to create role');
                }
            }

            setShowRoleForm(false);
            setEditingRole(null);
            setRoleForm({
                roleName: ''
            });
        } catch (error: any) {
            console.error('Failed to save role:', error);
            alert(error.message || 'Failed to save role');
        }
    };

    const deleteRole = async (roleId: string) => {
        if (!window.confirm('Are you sure you want to delete this role?')) return;

        try {
            const response = await roleApi.deleteRole(roleId);
            if (response.success) {
                alert('Role deleted successfully!');
                fetchRoles();
            } else {
                alert(response.message || 'Failed to delete role');
            }
        } catch (error: any) {
            console.error('Failed to delete role:', error);
            alert(error.message || 'Failed to delete role');
        }
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
                                {roles.map((role) => (
                                    <div key={role.userRoleId} className="col-12">
                                        <div className="card border border-light rounded-3 h-100">
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
                                                            onClick={() => openEditRole(role)}
                                                            className="btn btn-outline-primary btn-sm rounded-3"
                                                            title="Edit Role"
                                                        >
                                                            <Edit size={14} />
                                                        </button>
                                                        <button
                                                            onClick={() => deleteRole(role.userRoleId)}
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
                                            setRoleForm({
                                                roleName: ''
                                            });
                                        }}
                                    ></button>
                                </div>
                                <div className="modal-body">
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
                                                />
                                            </div>
                                        </div>
                                    </div>
                                </div>
                                <div className="modal-footer border-0 pt-0">
                                    <div className="d-flex gap-2">
                                        <button
                                            onClick={handleRoleSubmit}
                                            className="btn btn-primary rounded-3 fw-semibold"
                                            disabled={!roleForm.roleName.trim()}
                                        >
                                            {editingRole ? 'Update Role' : 'Create Role'}
                                        </button>
                                        <button
                                            onClick={() => {
                                                setShowRoleForm(false);
                                                setEditingRole(null);
                                                setRoleForm({
                                                    roleName: ''
                                                });
                                            }}
                                            className="btn btn-secondary rounded-3 fw-semibold"
                                        >
                                            Cancel
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};

export default Roles;
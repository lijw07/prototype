import React, { useState, useEffect } from 'react';
import { Shield, Plus, Edit, Trash2, Users, Key, CheckCircle2, AlertCircle } from 'lucide-react';

interface Role {
    roleId: string;
    roleName: string;
    description: string;
    permissions: string[];
    userCount: number;
    isActive: boolean;
    createdAt: string;
    updatedAt: string;
}

interface Permission {
    id: string;
    name: string;
    category: string;
}

const Roles: React.FC = () => {
    const [roles, setRoles] = useState<Role[]>([]);
    const [permissions, setPermissions] = useState<Permission[]>([]);
    const [loading, setLoading] = useState(false);
    const [showRoleForm, setShowRoleForm] = useState(false);
    const [editingRole, setEditingRole] = useState<Role | null>(null);

    const [roleForm, setRoleForm] = useState({
        roleName: '',
        description: '',
        permissions: [] as string[],
        isActive: true
    });

    // Mock permissions data
    const mockPermissions: Permission[] = [
        { id: 'users.view', name: 'View Users', category: 'User Management' },
        { id: 'users.create', name: 'Create Users', category: 'User Management' },
        { id: 'users.edit', name: 'Edit Users', category: 'User Management' },
        { id: 'users.delete', name: 'Delete Users', category: 'User Management' },
        { id: 'applications.view', name: 'View Applications', category: 'Application Management' },
        { id: 'applications.create', name: 'Create Applications', category: 'Application Management' },
        { id: 'applications.edit', name: 'Edit Applications', category: 'Application Management' },
        { id: 'applications.delete', name: 'Delete Applications', category: 'Application Management' },
        { id: 'roles.view', name: 'View Roles', category: 'Role Management' },
        { id: 'roles.create', name: 'Create Roles', category: 'Role Management' },
        { id: 'roles.edit', name: 'Edit Roles', category: 'Role Management' },
        { id: 'roles.delete', name: 'Delete Roles', category: 'Role Management' },
        { id: 'logs.view', name: 'View Logs', category: 'System' },
        { id: 'settings.view', name: 'View Settings', category: 'System' },
        { id: 'settings.edit', name: 'Edit Settings', category: 'System' }
    ];

    const fetchRoles = async () => {
        setLoading(true);
        try {
            const response = await fetch('/api/roles');
            if (response.ok) {
                const data = await response.json();
                setRoles(data);
            } else {
                // Mock data for demonstration
                setRoles([
                    {
                        roleId: '1',
                        roleName: 'Administrator',
                        description: 'Full system access with all permissions',
                        permissions: ['users.view', 'users.create', 'applications.view', 'roles.view', 'logs.view'],
                        userCount: 2,
                        isActive: true,
                        createdAt: new Date().toISOString(),
                        updatedAt: new Date().toISOString()
                    },
                    {
                        roleId: '2',
                        roleName: 'User Manager',
                        description: 'Manage users and view system information',
                        permissions: ['users.view', 'users.create', 'applications.view'],
                        userCount: 5,
                        isActive: true,
                        createdAt: new Date().toISOString(),
                        updatedAt: new Date().toISOString()
                    },
                    {
                        roleId: '3',
                        roleName: 'Viewer',
                        description: 'Read-only access to system information',
                        permissions: ['users.view', 'applications.view'],
                        userCount: 12,
                        isActive: true,
                        createdAt: new Date().toISOString(),
                        updatedAt: new Date().toISOString()
                    }
                ]);
            }
        } catch (error) {
            console.error('Failed to fetch roles:', error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchRoles();
        setPermissions(mockPermissions);
    }, []);

    const handleRoleSubmit = async () => {
        try {
            const roleData = {
                ...roleForm,
                roleId: editingRole?.roleId || crypto.randomUUID(),
                userCount: editingRole?.userCount || 0,
                createdAt: editingRole?.createdAt || new Date().toISOString(),
                updatedAt: new Date().toISOString()
            };

            if (editingRole) {
                // Update existing role
                setRoles(roles.map(role => role.roleId === editingRole.roleId ? roleData : role));
                alert('Role updated successfully!');
            } else {
                // Create new role
                setRoles([...roles, roleData]);
                alert('Role created successfully!');
            }

            setShowRoleForm(false);
            setEditingRole(null);
            setRoleForm({
                roleName: '',
                description: '',
                permissions: [],
                isActive: true
            });
        } catch (error: any) {
            console.error('Failed to save role:', error);
            alert('Failed to save role');
        }
    };

    const deleteRole = async (roleId: string) => {
        if (!window.confirm('Are you sure you want to delete this role?')) return;

        try {
            setRoles(roles.filter(role => role.roleId !== roleId));
            alert('Role deleted successfully!');
        } catch (error: any) {
            console.error('Failed to delete role:', error);
            alert('Failed to delete role');
        }
    };

    const togglePermission = (permissionId: string) => {
        setRoleForm(prev => ({
            ...prev,
            permissions: prev.permissions.includes(permissionId)
                ? prev.permissions.filter(p => p !== permissionId)
                : [...prev.permissions, permissionId]
        }));
    };

    const groupedPermissions = permissions.reduce((acc, permission) => {
        if (!acc[permission.category]) {
            acc[permission.category] = [];
        }
        acc[permission.category].push(permission);
        return acc;
    }, {} as Record<string, Permission[]>);

    const openEditRole = (role: Role) => {
        setEditingRole(role);
        setRoleForm({
            roleName: role.roleName,
            description: role.description,
            permissions: role.permissions,
            isActive: role.isActive
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
                                    <div key={role.roleId} className="col-12">
                                        <div className="card border border-light rounded-3 h-100">
                                            <div className="card-body p-4">
                                                <div className="d-flex justify-content-between align-items-start mb-3">
                                                    <div className="flex-grow-1">
                                                        <h5 className="card-title fw-bold text-dark mb-1 d-flex align-items-center" style={{wordWrap: 'break-word', overflowWrap: 'break-word'}}>
                                                            {role.roleName}
                                                            {role.isActive ? (
                                                                <CheckCircle2 className="text-success ms-2" size={16} />
                                                            ) : (
                                                                <AlertCircle className="text-warning ms-2" size={16} />
                                                            )}
                                                        </h5>
                                                        <p className="card-text text-muted small mb-2" style={{wordWrap: 'break-word', overflowWrap: 'break-word'}}>{role.description}</p>
                                                        <div className="d-flex align-items-center mb-2">
                                                            <Users size={14} className="text-muted me-1" />
                                                            <span className="small text-muted">{role.userCount} users assigned</span>
                                                        </div>
                                                        <div className="small text-muted">
                                                            {role.permissions.length} permissions
                                                        </div>
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
                                                            onClick={() => deleteRole(role.roleId)}
                                                            className="btn btn-outline-danger btn-sm rounded-3"
                                                            title="Delete Role"
                                                            disabled={role.userCount > 0}
                                                        >
                                                            <Trash2 size={14} />
                                                        </button>
                                                    </div>
                                                </div>
                                                
                                                {/* Permissions Preview */}
                                                <div className="border-top pt-3">
                                                    <h6 className="small fw-semibold text-muted mb-2">Permissions:</h6>
                                                    <div className="d-flex flex-wrap gap-1">
                                                        {role.permissions.slice(0, 3).map(permId => {
                                                            const perm = permissions.find(p => p.id === permId);
                                                            return perm ? (
                                                                <span key={permId} className="badge bg-light text-dark border small">
                                                                    {perm.name}
                                                                </span>
                                                            ) : null;
                                                        })}
                                                        {role.permissions.length > 3 && (
                                                            <span className="badge bg-secondary small">
                                                                +{role.permissions.length - 3} more
                                                            </span>
                                                        )}
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
                                                roleName: '',
                                                description: '',
                                                permissions: [],
                                                isActive: true
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
                                            <div className="mb-3">
                                                <label className="form-label fw-semibold">Description</label>
                                                <textarea
                                                    value={roleForm.description}
                                                    onChange={(e) => setRoleForm({...roleForm, description: e.target.value})}
                                                    className="form-control rounded-3"
                                                    rows={4}
                                                    placeholder="Enter role description"
                                                />
                                            </div>
                                            <div className="form-check">
                                                <input
                                                    className="form-check-input"
                                                    type="checkbox"
                                                    checked={roleForm.isActive}
                                                    onChange={(e) => setRoleForm({...roleForm, isActive: e.target.checked})}
                                                />
                                                <label className="form-check-label fw-semibold">
                                                    Active Role
                                                </label>
                                            </div>
                                        </div>
                                        <div className="col-12">
                                            <h5 className="fw-bold mb-3">Permissions</h5>
                                            <div className="border rounded-3 p-3" style={{maxHeight: '250px', overflowY: 'auto', overflowX: 'hidden', width: '100%'}}>
                                                {Object.entries(groupedPermissions).map(([category, perms]) => (
                                                    <div key={category} className="mb-3">
                                                        <h6 className="fw-semibold text-primary mb-2">{category}</h6>
                                                        {perms.map(permission => (
                                                            <div key={permission.id} className="form-check mb-1">
                                                                <input
                                                                    className="form-check-input"
                                                                    type="checkbox"
                                                                    checked={roleForm.permissions.includes(permission.id)}
                                                                    onChange={() => togglePermission(permission.id)}
                                                                />
                                                                <label className="form-check-label small">
                                                                    {permission.name}
                                                                </label>
                                                            </div>
                                                        ))}
                                                    </div>
                                                ))}
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
                                                    roleName: '',
                                                    description: '',
                                                    permissions: [],
                                                    isActive: true
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
import React from 'react';
import { Shield } from 'lucide-react';

const RolesMinimal: React.FC = () => {
    return (
        <div className="min-vh-100 bg-light">
            <div className="container-fluid py-4">
                {/* Header */}
                <div className="mb-4">
                    <div className="d-flex align-items-center mb-2">
                        <Shield className="text-primary me-3" size={32} />
                        <h1 className="display-5 fw-bold text-dark mb-0">Roles</h1>
                    </div>
                    <p className="text-muted fs-6">Manage user roles and their permissions</p>
                </div>

                {/* Simple Content */}
                <div className="card shadow-sm border-0 rounded-4">
                    <div className="card-body p-4">
                        <h2 className="card-title fw-bold text-dark mb-4">Simple roles content for testing width consistency</h2>
                        <p>This is a minimal version to test if the layout issue persists.</p>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default RolesMinimal;
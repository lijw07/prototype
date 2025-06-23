import React from 'react';
import { NavLink as RouterLink, useLocation } from 'react-router-dom';
import { Home, Users, Database, Shield, Activity, FileText, Key, Monitor, Briefcase, UserPlus, Award } from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';
import './NavMenu.css';

interface NavItem {
  path: string;
  label: string;
  icon: React.ComponentType<any>;
}

const navItems: NavItem[] = [
  { path: '/dashboard', label: 'Dashboard', icon: Home },
  { path: '/security-dashboard', label: 'Security Dashboard', icon: Shield },
  { path: '/system-health', label: 'System Health', icon: Monitor },
  { path: '/executive-dashboard', label: 'Executive Dashboard', icon: Briefcase },
  { path: '/user-provisioning', label: 'User Provisioning', icon: UserPlus },
  { path: '/compliance', label: 'Compliance', icon: Award },
  { path: '/accounts', label: 'Accounts', icon: Users },
  { path: '/applications', label: 'Applications', icon: Database },
  { path: '/roles', label: 'Roles', icon: Key },
  { path: '/audit-logs', label: 'Audit Logs', icon: Activity },
  { path: '/activity-logs', label: 'Activity Logs', icon: Activity },
  { path: '/application-logs', label: 'Application Logs', icon: FileText },
];

export default function NavMenu() {
  const location = useLocation();
  const { isAuthenticated } = useAuth();

  // Don't show nav menu on login/signup pages or when not authenticated
  if (!isAuthenticated) {
    return null;
  }

  return (
    <nav className="bg-light border-end d-flex flex-column" style={{ 
      width: '220px', 
      minWidth: '220px', 
      maxWidth: '220px', 
      minHeight: '100%',
      flexShrink: 0,
      overflow: 'hidden'
    }}>
      <div className="p-3">
        <h6 className="text-muted text-uppercase small fw-bold mb-3">Navigation</h6>
        <ul className="nav flex-column">
          {navItems.map((item) => {
            const Icon = item.icon;
            const isActive = location.pathname === item.path;
            
            return (
              <li key={item.path} className="nav-item mb-1">
                <RouterLink
                  to={item.path}
                  className={`nav-link d-flex align-items-center px-3 py-2 rounded ${
                    isActive 
                      ? 'bg-primary text-white' 
                      : 'text-dark hover-bg-light'
                  }`}
                  style={{ textDecoration: 'none' }}
                >
                  <Icon size={18} className="me-2" />
                  <span>{item.label}</span>
                </RouterLink>
              </li>
            );
          })}
        </ul>
      </div>
      
      {/* Version info */}
      <div className="mt-auto p-3 border-top">
        <small className="text-muted">
          CAMS v1.0
        </small>
      </div>
    </nav>
  );
}

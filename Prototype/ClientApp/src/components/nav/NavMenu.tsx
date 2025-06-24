import React from 'react';
import { NavLink as RouterLink, useLocation } from 'react-router-dom';
import { Home, Users, Database, Shield, Activity, FileText, Key, Monitor, Briefcase, UserPlus, Award } from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';
import './NavMenu.css';

interface NavItem {
  path: string;
  label: string;
  icon: React.ComponentType<any>;
  allowedRoles: string[];
}
interface NavSection {
  title: string;
  items: NavItem[];
}

const navSections: NavSection[] = [
  {
    title: 'Dashboards',
    items: [
      { path: '/dashboard', label: 'Home', icon: Home, allowedRoles: ['User', 'Admin', 'Platform Admin'] },
      { path: '/compliance', label: 'Compliance', icon: Award, allowedRoles: ['User', 'Admin', 'Platform Admin'] },
      { path: '/executive-dashboard', label: 'Executive', icon: Briefcase, allowedRoles: ['User', 'Admin', 'Platform Admin'] },
      { path: '/security-dashboard', label: 'Security', icon: Shield, allowedRoles: ['User', 'Admin', 'Platform Admin'] },

    ],
  },
  {
    title: 'Personnel',
    items: [
      { path: '/accounts', label: 'Accounts', icon: Users, allowedRoles: ['User', 'Admin', 'Platform Admin'] },
      { path: '/applications', label: 'Applications', icon: Database, allowedRoles: ['User', 'Admin', 'Platform Admin'] },
      { path: '/user-provisioning', label: 'User Provisioning', icon: UserPlus, allowedRoles: ['User', 'Admin', 'Platform Admin'] },
    ],
  },
  {
    title: 'Logs',
    items: [
      { path: '/activity-logs', label: 'Activities', icon: Activity, allowedRoles: ['User', 'Admin', 'Platform Admin'] },
      { path: '/application-logs', label: 'Applications', icon: FileText, allowedRoles: ['User', 'Admin', 'Platform Admin'] },
      { path: '/audit-logs', label: 'Audits', icon: Activity, allowedRoles: ['User', 'Admin', 'Platform Admin'] }
    ],
  },
  {
    title: 'Administrative',
    items : [
      { path: '/roles', label: 'Roles', icon: Key, allowedRoles: ['Platform Admin'] },
      { path: '/system-health', label: 'System Health', icon: Monitor, allowedRoles: ['Platform Admin'] },
    ]
  }
];

export default function NavMenu() {
  const location = useLocation();
  const { user, isAuthenticated } = useAuth();

  const [openSections, setOpenSections] = React.useState<Record<string, boolean>>({
    Dashboards: true, // default open
  });

  const toggleSection = React.useCallback((title: string) => {
    setOpenSections((prev) => ({
      ...prev,
      [title]: !prev[title],
    }));
  }, []);

  if (!isAuthenticated) {
    return null;
  }

  return (
    <nav
      className="bg-light border-end d-flex flex-column"
      style={{
        width: '220px',
        minWidth: '220px',
        maxWidth: '220px',
        minHeight: '100%',
        flexShrink: 0,
        overflow: 'hidden',
      }}
    >
      <div className="p-3">
        <h6 className="text-muted text-uppercase small fw-bold mb-3">Navigation</h6>

        {navSections.map((section) => {
          const isOpen = openSections[section.title] ?? false; // default closed if not in state
          const visibleItems = section.items.filter(item => item.allowedRoles.includes(user?.role ?? ''));

            if (visibleItems.length === 0) {
              return null; // hide this entire section/tab group
            } else 
            {
                        return (
            <div key={section.title} className="mb-2">
              <button
                type="button"
                onClick={(e) => {
                  e.preventDefault();
                  e.stopPropagation();
                  toggleSection(section.title);
                }}
                className="btn btn-light w-100 d-flex justify-content-between align-items-center px-3 py-2 mb-1"
              >
                <span className="text-start">{section.title}</span>
                <span>{isOpen ? '▼' : '▶'}</span>
              </button>

            {isOpen && (
              <ul className="nav flex-column ps-3">
                {section.items
                  .filter(item => item.allowedRoles.includes(user?.role ?? ''))
                  .map((item) => {
                    const Icon = item.icon;
                    const isActive = location.pathname === item.path;

                    return (
                      <li key={item.path} className="nav-item mb-1">
                        <RouterLink
                          to={item.path}
                          className={`nav-link d-flex align-items-center px-3 py-2 rounded ${
                            isActive ? 'bg-primary text-white' : 'text-dark hover-bg-light'
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
            )}
            </div>
              );
            }
        })}
      </div>
      <div className="mt-auto p-3 border-top">
        <small className="text-muted">CAMS v1.0</small>
      </div>
    </nav>
  );
}
/*
{navSections.map((section) => {
  const visibleItems = section.items.filter(item => item.allowedRoles.includes(user?.role ?? ''));

  if (visibleItems.length === 0) {
    return null; // hide this entire section/tab group
  }

  const isOpen = openSections[section.title] ?? false;

  return (
    <div key={section.title} className="mb-2">
      <button
        type="button"
        onClick={(e) => {
          e.preventDefault();
          e.stopPropagation();
          toggleSection(section.title);
        }}
        className="btn btn-light w-100 d-flex justify-content-between align-items-center px-3 py-2 mb-1"
      >
        <span className="text-start">{section.title}</span>
        <span>{isOpen ? '▼' : '▶'}</span>
      </button>

      {isOpen && (
        <ul className="nav flex-column ps-3">
          {visibleItems.map((item) => {
            const Icon = item.icon;
            const isActive = location.pathname === item.path;

            return (
              <li key={item.path} className="nav-item mb-1">
                <RouterLink
                  to={item.path}
                  className={`nav-link d-flex align-items-center px-3 py-2 rounded ${
                    isActive ? 'bg-primary text-white' : 'text-dark hover-bg-light'
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
      )}
    </div>
  );
})}
*/
import React, { ReactNode, useState, useEffect, useRef } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { User, Settings, LogOut, ChevronDown, Mail } from 'lucide-react';
import NavMenu from './nav/NavMenu';
import { useAuth } from '../contexts/AuthContext';
import WebsiteLayout from './website/WebsiteLayout';
import { NotificationDropdown } from './common/NotificationDropdown';

interface LayoutProps {
  children: ReactNode; // Typing children correctly
}

export default function Layout({ children }: LayoutProps) {
  const { user, isAuthenticated, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [showUserDropdown, setShowUserDropdown] = useState(false);
  const [showNotificationDropdown, setShowNotificationDropdown] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const notificationRef = useRef<HTMLDivElement>(null);
  
  // Define which routes should use the website layout (public pages)
  const publicRoutes = ['/home', '/', '/about', '/contact', '/services', '/solutions', '/pricing'];
  const shouldUseWebsiteLayout = publicRoutes.some(route => 
    route === '/' ? location.pathname === '/' : location.pathname.startsWith(route)
  );
  
  // List of routes where login button should be hidden (but header still shows)
  const hideLoginButtonRoutes = ['/login', '/sign-up', '/verify', '/reset-password'];
  const shouldHideLoginButton = hideLoginButtonRoutes.includes(location.pathname);
  
  // List of routes where entire header and navigation should be hidden
  const hideHeaderRoutes = ['/verify-email'];
  const shouldHideHeader = hideHeaderRoutes.some(route => location.pathname.startsWith(route));

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  // Close dropdowns when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setShowUserDropdown(false);
      }
      if (notificationRef.current && !notificationRef.current.contains(event.target as Node)) {
        setShowNotificationDropdown(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, []);

  // If it's a public route, use the website layout
  if (shouldUseWebsiteLayout) {
    return <WebsiteLayout>{children}</WebsiteLayout>;
  }

  return (
    <div className="d-flex flex-column vh-100">
      {/* Regular header - Hidden on verification pages */}
      {!shouldHideHeader && (
        <header
          className="bg-white border-bottom d-flex justify-content-between align-items-center px-5 py-3 shadow-sm"
          style={{ minHeight: '80px' }}
        >
        <div className="d-flex align-items-center">
          <div className="me-3">
            <div className="rounded-3 bg-dark p-2 d-flex align-items-center justify-content-center" style={{ width: '48px', height: '48px' }}>
              <span className="fw-bold text-white fs-4">C</span>
            </div>
          </div>
          <div>
            <h3 className="mb-0 fw-bold text-dark">Centralized Application Management</h3>
            <small className="text-muted">Enterprise Data Access Platform</small>
          </div>
        </div>
        <div>
          {isAuthenticated ? (
            <div className="d-flex align-items-center gap-2">
              {/* Notification Dropdown */}
              <div ref={notificationRef}>
                <NotificationDropdown 
                  isOpen={showNotificationDropdown}
                  onToggle={() => setShowNotificationDropdown(!showNotificationDropdown)}
                />
              </div>
              
              <div className="position-relative" ref={dropdownRef}>
              <button
                className="btn btn-link p-0 border-0 d-flex align-items-center text-decoration-none"
                onClick={() => setShowUserDropdown(!showUserDropdown)}
                style={{ background: 'none' }}
              >
                <div className="d-flex align-items-center">
                  <div className="me-2">
                    <div className="rounded-circle bg-light p-2">
                      <User size={20} className="text-dark" />
                    </div>
                  </div>
                  <div className="text-start me-2">
                    <div className="fw-semibold text-dark">{user?.username}</div>
                    <small className="text-muted">Administrator</small>
                  </div>
                  <ChevronDown size={16} className="text-muted" />
                </div>
              </button>
              
              {showUserDropdown && (
                <div 
                  className="position-absolute end-0 mt-2 bg-white border rounded shadow-lg"
                  style={{ minWidth: '200px', zIndex: 1000 }}
                >
                  <div className="p-2">
                    <button
                      className="btn btn-light w-100 d-flex align-items-center justify-content-start mb-1"
                      onClick={() => {
                        navigate('/settings');
                        setShowUserDropdown(false);
                      }}
                    >
                      <Settings size={16} className="me-2" />
                      Settings
                    </button>
                    <button
                      className="btn btn-outline-danger w-100 d-flex align-items-center justify-content-start"
                      onClick={() => {
                        handleLogout();
                        setShowUserDropdown(false);
                      }}
                    >
                      <LogOut size={16} className="me-2" />
                      Logout
                    </button>
                  </div>
                </div>
              )}
              </div>
            </div>
          ) : !shouldHideLoginButton ? (
            <button 
              className="btn btn-primary btn-sm fw-semibold"
              onClick={() => navigate('/login')}
            >
              Login
            </button>
          ) : null}
        </div>
        </header>
      )}

      {/* Sidebar + Main */}
      <div className="d-flex flex-grow-1 overflow-hidden">
        {isAuthenticated && !shouldHideHeader && <NavMenu />}
        <main 
          className={`flex-grow-1 overflow-auto ${
            isAuthenticated && !shouldHideHeader ? 'p-4 bg-light' : 
            shouldHideHeader ? '' : ''
          }`}
          style={{ 
            minWidth: 0, 
            width: isAuthenticated && !shouldHideHeader ? 'calc(100% - 220px)' : '100%' 
          }}
        >
          {children}
        </main>
      </div>
      
    </div>
  );
}

import React, { ReactNode, useState, useEffect, useRef } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { User, Settings, LogOut, ChevronDown, Mail } from 'lucide-react';
import NavMenu from './nav/NavMenu';
import { useAuth } from '../contexts/AuthContext';

interface LayoutProps {
  children: ReactNode; // Typing children correctly
}

export default function Layout({ children }: LayoutProps) {
  const { user, isAuthenticated, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [showUserDropdown, setShowUserDropdown] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  
  // List of routes where login button should be hidden (but header still shows)
  const hideLoginButtonRoutes = ['/login', '/sign-up', '/verify', '/reset-password'];
  const shouldHideLoginButton = hideLoginButtonRoutes.includes(location.pathname);
  
  // List of routes where entire header and navigation should be hidden
  const hideHeaderRoutes = ['/verify-email'];
  const shouldHideHeader = hideHeaderRoutes.some(route => location.pathname.startsWith(route));
  
  // Check if we're on the home page to show minimal header
  const isHomePage = location.pathname === '/home';

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  // Render minimal header for home page
  const renderHomeHeader = () => (
    <header className="position-absolute top-0 start-0 w-100 bg-transparent" style={{ zIndex: 1000 }}>
      <nav className="container-fluid px-4 py-4">
        <div className="d-flex justify-content-between align-items-center">
          {/* Logo */}
          <div className="d-flex align-items-center">
            <div className="me-3">
              <div className="rounded-3 bg-white shadow-sm p-2 d-flex align-items-center justify-content-center transition-all" 
                   style={{ 
                     width: '42px', 
                     height: '42px',
                     transition: 'all 0.3s ease'
                   }}>
                <span className="fw-bold text-dark fs-5">C</span>
              </div>
            </div>
            <div>
              <span className="fw-bold text-white fs-4 d-block lh-1">CAMS</span>
              <small className="text-white opacity-75">Enterprise Access Management</small>
            </div>
          </div>

          {/* Navigation buttons */}
          <div className="d-flex align-items-center gap-3">
            <button 
              className="btn btn-outline-light btn-sm d-flex align-items-center fw-semibold px-3 py-2 border-2"
              onClick={() => window.open('mailto:contact@cams.com', '_blank')}
              style={{
                borderColor: 'rgba(255,255,255,0.3)',
                transition: 'all 0.3s ease'
              }}
              onMouseEnter={(e) => {
                const target = e.target as HTMLButtonElement;
                target.style.backgroundColor = 'rgba(255,255,255,0.1)';
                target.style.borderColor = 'rgba(255,255,255,0.8)';
              }}
              onMouseLeave={(e) => {
                const target = e.target as HTMLButtonElement;
                target.style.backgroundColor = 'transparent';
                target.style.borderColor = 'rgba(255,255,255,0.3)';
              }}
            >
              <Mail size={16} className="me-2" />
              Contact
            </button>
            {!isAuthenticated && (
              <button 
                className="btn btn-warning btn-sm fw-semibold px-4 py-2 border-0 shadow-sm"
                onClick={() => navigate('/login')}
                style={{
                  transition: 'all 0.3s ease'
                }}
                onMouseEnter={(e) => {
                  const target = e.target as HTMLButtonElement;
                  target.style.transform = 'translateY(-1px)';
                  target.style.boxShadow = '0 4px 8px rgba(0,0,0,0.2)';
                }}
                onMouseLeave={(e) => {
                  const target = e.target as HTMLButtonElement;
                  target.style.transform = 'translateY(0)';
                  target.style.boxShadow = '0 2px 4px rgba(0,0,0,0.1)';
                }}
              >
                Login
              </button>
            )}
            {isAuthenticated && (
              <button 
                className="btn btn-warning btn-sm fw-semibold px-4 py-2 border-0 shadow-sm"
                onClick={() => navigate('/dashboard')}
                style={{
                  transition: 'all 0.3s ease'
                }}
                onMouseEnter={(e) => {
                  const target = e.target as HTMLButtonElement;
                  target.style.transform = 'translateY(-1px)';
                  target.style.boxShadow = '0 4px 8px rgba(0,0,0,0.2)';
                }}
                onMouseLeave={(e) => {
                  const target = e.target as HTMLButtonElement;
                  target.style.transform = 'translateY(0)';
                  target.style.boxShadow = '0 2px 4px rgba(0,0,0,0.1)';
                }}
              >
                Dashboard
              </button>
            )}
          </div>
        </div>
      </nav>
    </header>
  );

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setShowUserDropdown(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, []);

  return (
    <div className="d-flex flex-column vh-100">
      {/* Home page gets special header */}
      {isHomePage && renderHomeHeader()}
      
      {/* Regular header - Hidden on verification pages and home page */}
      {!shouldHideHeader && !isHomePage && (
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
        {isAuthenticated && !shouldHideHeader && !isHomePage && <NavMenu />}
        <main 
          className={`flex-grow-1 overflow-auto ${
            isHomePage ? '' : 
            isAuthenticated && !shouldHideHeader ? 'p-4 bg-light' : 
            shouldHideHeader ? '' : ''
          }`}
          style={{ 
            minWidth: 0, 
            width: isHomePage ? '100%' : 
                   isAuthenticated && !shouldHideHeader ? 'calc(100% - 220px)' : '100%' 
          }}
        >
          {children}
        </main>
      </div>
    </div>
  );
}

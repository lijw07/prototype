import React, { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { 
  Menu, 
  X, 
  Shield, 
  ChevronDown,
  Phone,
  Mail,
  ArrowRight
} from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';

export default function WebsiteHeader() {
  const navigate = useNavigate();
  const location = useLocation();
  const { user } = useAuth();
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [isServicesDropdownOpen, setIsServicesDropdownOpen] = useState(false);

  const isHomePage = location.pathname === '/' || location.pathname === '/home';

  const navigationItems = [
    { name: 'Home', path: '/home', exact: true },
    { name: 'About', path: '/about' },
    { 
      name: 'Services', 
      path: '/services',
      hasDropdown: true,
      dropdownItems: [
        { name: 'Employee Access Management', path: '/services/access-management' },
        { name: 'Database Integration', path: '/services/database-integration' },
        { name: 'API Connectivity', path: '/services/api-connectivity' },
        { name: 'Compliance & Security', path: '/services/compliance' },
        { name: 'Custom Integration', path: '/services/custom-integration' }
      ]
    },
    { name: 'Solutions', path: '/solutions' },
    { name: 'Pricing', path: '/pricing' },
    { name: 'Contact', path: '/contact' }
  ];

  const isActivePath = (item: any) => {
    if (item.exact) {
      return location.pathname === item.path || (location.pathname === '/' && item.path === '/home');
    }
    return location.pathname.startsWith(item.path);
  };

  const headerClasses = isHomePage 
    ? "position-fixed w-100 top-0 start-0 py-3 border-0" 
    : "sticky-top bg-white shadow-sm py-3 border-bottom";

  const brandClasses = isHomePage 
    ? "text-white fw-bold" 
    : "text-primary fw-bold";

  const navLinkClasses = (item: any) => {
    const baseClasses = "nav-link fw-medium px-3 py-2 rounded-2 text-decoration-none d-flex align-items-center";
    const homePageClasses = isHomePage ? "text-white" : "text-dark";
    const activeClasses = isActivePath(item) ? (isHomePage ? "bg-white bg-opacity-20" : "bg-primary bg-opacity-10 text-primary") : "";
    return `${baseClasses} ${homePageClasses} ${activeClasses}`;
  };

  const buttonClasses = isHomePage 
    ? "btn btn-warning fw-semibold rounded-3 px-4 py-2"
    : "btn btn-primary fw-semibold rounded-3 px-4 py-2";

  return (
    <header className={headerClasses} style={{ zIndex: 1000 }}>
      {isHomePage && (
        <div className="position-absolute top-0 start-0 w-100 h-100 bg-dark bg-opacity-25"></div>
      )}
      
      <div className="container position-relative">
        <nav className="navbar navbar-expand-lg navbar-dark p-0">
          <div className="d-flex align-items-center w-100">
            {/* Brand */}
            <button 
              className={`navbar-brand border-0 bg-transparent p-0 ${brandClasses}`}
              onClick={() => navigate('/home')}
              style={{ fontSize: '1.75rem' }}
            >
              <Shield className="me-2" size={32} />
              <span>CAMS</span>
            </button>

            {/* Desktop Navigation */}
            <div className="d-none d-lg-flex align-items-center mx-auto">
              <ul className="navbar-nav d-flex flex-row gap-1">
                {navigationItems.map((item) => (
                  <li key={item.name} className="nav-item position-relative">
                    {item.hasDropdown ? (
                      <div 
                        className="dropdown"
                        onMouseEnter={() => setIsServicesDropdownOpen(true)}
                        onMouseLeave={() => setIsServicesDropdownOpen(false)}
                      >
                        <button
                          className={navLinkClasses(item) + " dropdown-toggle border-0 bg-transparent"}
                          type="button"
                        >
                          {item.name}
                          <ChevronDown size={16} className="ms-1" />
                        </button>
                        
                        {isServicesDropdownOpen && (
                          <div className="dropdown-menu show position-absolute top-100 start-0 mt-2 py-2 border-0 shadow-lg rounded-3 bg-white" style={{ minWidth: '280px' }}>
                            {item.dropdownItems?.map((dropdownItem) => (
                              <button
                                key={dropdownItem.name}
                                className="dropdown-item py-2 px-3 border-0 bg-transparent text-dark fw-medium"
                                onClick={() => {
                                  navigate(dropdownItem.path);
                                  setIsServicesDropdownOpen(false);
                                }}
                              >
                                {dropdownItem.name}
                              </button>
                            ))}
                          </div>
                        )}
                      </div>
                    ) : (
                      <button
                        className={navLinkClasses(item) + " border-0 bg-transparent"}
                        onClick={() => navigate(item.path)}
                      >
                        {item.name}
                      </button>
                    )}
                  </li>
                ))}
              </ul>
            </div>

            {/* CTA Buttons */}
            <div className="d-none d-lg-flex align-items-center gap-3">
              <button 
                className={`btn btn-outline-${isHomePage ? 'light' : 'primary'} fw-medium rounded-3 px-4 py-2 d-flex align-items-center`}
                onClick={() => window.location.href = 'tel:+1-800-CAMS-247'}
              >
                <Phone size={16} className="me-2" />
                Call Sales
              </button>
              
              {user ? (
                <button 
                  className={buttonClasses + " d-flex align-items-center"}
                  onClick={() => navigate('/dashboard')}
                >
                  Dashboard
                  <ArrowRight size={16} className="ms-2" />
                </button>
              ) : (
                <button 
                  className={buttonClasses + " d-flex align-items-center"}
                  onClick={() => navigate('/login')}
                >
                  Get Started
                  <ArrowRight size={16} className="ms-2" />
                </button>
              )}
            </div>

            {/* Mobile Menu Button */}
            <button 
              className={`d-lg-none btn border-0 bg-transparent p-2 ${isHomePage ? 'text-white' : 'text-dark'}`}
              onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
            >
              {isMobileMenuOpen ? <X size={24} /> : <Menu size={24} />}
            </button>
          </div>

          {/* Mobile Navigation */}
          {isMobileMenuOpen && (
            <div className="d-lg-none position-absolute top-100 start-0 w-100 bg-white shadow-lg rounded-bottom-3 py-4 mt-2">
              <div className="container">
                <ul className="navbar-nav gap-2">
                  {navigationItems.map((item) => (
                    <li key={item.name} className="nav-item">
                      {item.hasDropdown ? (
                        <div>
                          <button
                            className="nav-link text-dark fw-medium py-2 px-3 w-100 text-start border-0 bg-transparent d-flex align-items-center justify-content-between"
                            onClick={() => setIsServicesDropdownOpen(!isServicesDropdownOpen)}
                          >
                            {item.name}
                            <ChevronDown size={16} className={`transition-transform ${isServicesDropdownOpen ? 'rotate-180' : ''}`} />
                          </button>
                          
                          {isServicesDropdownOpen && (
                            <div className="ps-3 py-2">
                              {item.dropdownItems?.map((dropdownItem) => (
                                <button
                                  key={dropdownItem.name}
                                  className="nav-link text-muted fw-medium py-2 px-3 border-0 bg-transparent w-100 text-start"
                                  onClick={() => {
                                    navigate(dropdownItem.path);
                                    setIsMobileMenuOpen(false);
                                  }}
                                >
                                  {dropdownItem.name}
                                </button>
                              ))}
                            </div>
                          )}
                        </div>
                      ) : (
                        <button
                          className={`nav-link fw-medium py-2 px-3 w-100 text-start border-0 bg-transparent ${
                            isActivePath(item) ? 'text-primary bg-primary bg-opacity-10' : 'text-dark'
                          }`}
                          onClick={() => {
                            navigate(item.path);
                            setIsMobileMenuOpen(false);
                          }}
                        >
                          {item.name}
                        </button>
                      )}
                    </li>
                  ))}
                </ul>
                
                <div className="border-top pt-4 mt-4">
                  <div className="d-flex flex-column gap-3">
                    <button 
                      className="btn btn-outline-primary fw-medium rounded-3 py-2 d-flex align-items-center justify-content-center"
                      onClick={() => window.location.href = 'tel:+1-800-CAMS-247'}
                    >
                      <Phone size={16} className="me-2" />
                      Call Sales
                    </button>
                    
                    {user ? (
                      <button 
                        className="btn btn-primary fw-semibold rounded-3 py-2 d-flex align-items-center justify-content-center"
                        onClick={() => {
                          navigate('/dashboard');
                          setIsMobileMenuOpen(false);
                        }}
                      >
                        Dashboard
                        <ArrowRight size={16} className="ms-2" />
                      </button>
                    ) : (
                      <button 
                        className="btn btn-primary fw-semibold rounded-3 py-2 d-flex align-items-center justify-content-center"
                        onClick={() => {
                          navigate('/login');
                          setIsMobileMenuOpen(false);
                        }}
                      >
                        Get Started
                        <ArrowRight size={16} className="ms-2" />
                      </button>
                    )}
                  </div>
                </div>
              </div>
            </div>
          )}
        </nav>
      </div>
    </header>
  );
}
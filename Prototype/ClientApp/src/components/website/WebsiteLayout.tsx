import React from 'react';
import { useLocation } from 'react-router-dom';
import WebsiteHeader from './WebsiteHeader';
import WebsiteFooter from './WebsiteFooter';

interface WebsiteLayoutProps {
  children: React.ReactNode;
}

export default function WebsiteLayout({ children }: WebsiteLayoutProps) {
  const location = useLocation();
  
  // Define which routes should use the website layout (public pages)
  const publicRoutes = ['/home', '/', '/about', '/contact', '/services', '/solutions', '/pricing'];
  
  // Check if current route should use website layout
  const shouldUseWebsiteLayout = publicRoutes.some(route => 
    route === '/' ? location.pathname === '/' : location.pathname.startsWith(route)
  );

  if (!shouldUseWebsiteLayout) {
    return <>{children}</>;
  }

  return (
    <div className="d-flex flex-column min-vh-100">
      <WebsiteHeader />
      <main className="flex-grow-1">
        {children}
      </main>
      <WebsiteFooter />
    </div>
  );
}
import React from 'react';
import { Container } from 'reactstrap';
import NavMenu from './NavMenu';

export default function Layout({ children }) {
  return (
    <div className="d-flex flex-column vh-100">
      {/* Bigger Top Header */}
      <header className="bg-white border-bottom d-flex justify-content-between align-items-center px-5 py-3 shadow-sm" style={{ minHeight: '80px' }}>
        <h4 className="mb-0 fw-bold">Sentinel Prototype</h4>
        <div>
          <button className="btn btn-outline-secondary btn-sm">Login</button>
        </div>
      </header>

      {/* Sidebar + Main */}
      <div className="d-flex flex-grow-1 overflow-hidden">
        <NavMenu />
        <main className="flex-grow-1 overflow-auto p-4 bg-light">
          {children}
        </main>
      </div>
    </div>
  );
}

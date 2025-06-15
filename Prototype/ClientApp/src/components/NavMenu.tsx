import React from 'react';
import { NavLink as RouterLink } from 'react-router-dom';
import { Nav, NavItem, NavLink } from 'reactstrap';
import './NavMenu.css';

export default function NavMenu() {
  return (
    <nav className="bg-light border-end p-3" style={{ width: '200px' }}>
      <Nav vertical className="w-100 text-center">
        <NavItem>
          <NavLink tag={RouterLink} to="/dashboard" className="text-dark">
            Dashboard
          </NavLink>
        </NavItem>
        <NavItem>
          <NavLink tag={RouterLink} to="/accounts" className="text-dark">
            Accounts
          </NavLink>
        </NavItem>
        <NavItem>
          <NavLink tag={RouterLink} to="/login" className="text-dark">
            Login
          </NavLink>
        </NavItem>
      </Nav>
    </nav>
  );
}

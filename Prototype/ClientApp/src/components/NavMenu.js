import { NavLink as Link } from 'react-router-dom';
import { Nav, NavItem, NavLink } from 'reactstrap';
import './NavMenu.css';

export default function NavMenu() {
  return (
    <nav className="bg-light border-end p-3" style={{ width: '200px' }}>
      <Nav vertical className="w-100 text-center">
        <NavItem>
          <NavLink tag={Link} to="/" className="text-dark">Home</NavLink>
        </NavItem>
        <NavItem>
          <NavLink tag={Link} to="/counter" className="text-dark">Counter</NavLink>
        </NavItem>
        <NavItem>
          <NavLink tag={Link} to="/fetch-data" className="text-dark">Fetch Data</NavLink>
        </NavItem>
        <NavItem>
          <NavLink tag={Link} to="/accounts" className="text-dark">Accounts</NavLink>
        </NavItem>
        <NavItem>
          <NavLink tag={Link} to="/login" className="text-dark">Login</NavLink>
        </NavItem>
      </Nav>
    </nav>
  );
}

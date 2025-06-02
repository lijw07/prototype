import React from 'react';
import { Container } from 'reactstrap';
import NavMenu from './NavMenu';

export default function Layout({ children }) {
  return (
    <>
      <NavMenu />
      <Container>
        {children}
      </Container>
    </>
  );
}
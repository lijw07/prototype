import React from 'react';
import { Routes, Route } from 'react-router-dom';
import Layout from './components/Layout';
import Accounts from './components/account/Accounts';
import Login from './components/login/Login';
import Signup from './components/login/Signup';
import Dashboard from './components/dashboard/Dashboard';

export default function App() {
  return (
    <Layout>
      <Routes>
        <Route path="/dashboard" element={<Dashboard />} />
        <Route path="/accounts" element={<Accounts />} />
        <Route path="/login" element={<Login />} />
        <Route path="/sign-up" element={<Signup />} />
      </Routes>
    </Layout>
  );
}

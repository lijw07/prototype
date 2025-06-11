import React from 'react';
import { Routes, Route } from 'react-router-dom';
import Layout from './components/Layout';
import Home from './components/Home';
import Counter from './components/Counter';
import FetchData from './components/FetchData';
import Accounts from './components/Accounts';
import Login from './components/login/Login';
import Signup from './components/login/Signup';
import Dashboard from './components/Dashboard'

export default function App() {
  return (
    <Layout>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/counter" element={<Counter />} />
        <Route path="/fetch-data" element={<FetchData />} />
        <Route path="/accounts" element={<Accounts />} />
        <Route path="/login" element={<Login />} />
        <Route path="/sign-up" element={<Signup />} />
      </Routes>
    </Layout>
  );
}
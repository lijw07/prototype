import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import Layout from './components/Layout';
import ProtectedRoute from './components/auth/ProtectedRoute';
import Accounts from './components/account/Accounts';
import Login from './components/login/Login';
import Signup from './components/login/Signup';
import Dashboard from './components/dashboard/Dashboard';
import Settings from './components/settings/Settings';
import VerifyEmailPage from "./components/account/VerifyEmailPage";
import ResetPassword from "./components/account/ResetPassword";
import AuditLogs from './components/audit/AuditLogs';
import ActivityLogs from './components/activity/ActivityLogs';
import Applications from './components/applications/Applications';
import ApplicationLogs from './components/application-logs/ApplicationLogs';
import Roles from './components/roles/Roles';

export default function App() {
  return (
    <AuthProvider>
      <Layout>
        <Routes>
          {/* Public routes */}
          <Route path="/login" element={<Login />} />
          <Route path="/verify" element={<VerifyEmailPage />} />
          <Route path="/reset-password" element={<ResetPassword />} />
          
          {/* Protected routes */}
          <Route path="/dashboard" element={
            <ProtectedRoute>
              <Dashboard />
            </ProtectedRoute>
          } />
          <Route path="/accounts" element={
            <ProtectedRoute>
              <Accounts />
            </ProtectedRoute>
          } />
          <Route path="/settings" element={
            <ProtectedRoute>
              <Settings />
            </ProtectedRoute>
          } />
          <Route path="/audit-logs" element={
            <ProtectedRoute>
              <AuditLogs />
            </ProtectedRoute>
          } />
          <Route path="/activity-logs" element={
            <ProtectedRoute>
              <ActivityLogs />
            </ProtectedRoute>
          } />
          <Route path="/applications" element={
            <ProtectedRoute>
              <Applications />
            </ProtectedRoute>
          } />
          <Route path="/application-logs" element={
            <ProtectedRoute>
              <ApplicationLogs />
            </ProtectedRoute>
          } />
          <Route path="/roles" element={
            <ProtectedRoute>
              <Roles />
            </ProtectedRoute>
          } />
          
          {/* Default redirect */}
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </Layout>
    </AuthProvider>
  );
}

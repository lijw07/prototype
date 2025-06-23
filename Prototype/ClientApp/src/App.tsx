import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './contexts/AuthContext';
import Layout from './components/Layout';
import ProtectedRoute from './components/auth/ProtectedRoute';
import Accounts from './components/account/Accounts';
import Login from './components/login/Login';
import Dashboard from './components/dashboard/Dashboard';
import Settings from './components/settings/Settings';
import VerifyEmailPage from "./components/account/VerifyEmailPage";
import ResetPassword from "./components/account/ResetPassword";
import AuditLogs from './components/audit/AuditLogs';
import ActivityLogs from './components/activity/ActivityLogs';
import Applications from './components/applications/Applications';
import ApplicationLogs from './components/application-logs/ApplicationLogs';
import Roles from './components/roles/Roles';
import SecurityDashboard from './components/security/SecurityDashboard';
import SystemHealthDashboard from './components/health/SystemHealthDashboard';
import ExecutiveDashboard from './components/executive/ExecutiveDashboard';
import UserProvisioning from './components/provisioning/UserProvisioning';
import ComplianceDashboard from './components/compliance/ComplianceDashboard';
import Home from './components/home/Home';

// Conditional redirect component
function ConditionalRedirect() {
  const { isAuthenticated, loading } = useAuth();
  
  if (loading) {
    return (
      <div className="min-vh-100 d-flex align-items-center justify-content-center">
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Loading...</span>
        </div>
      </div>
    );
  }
  
  return <Navigate to={isAuthenticated ? "/dashboard" : "/home"} replace />;
}

export default function App() {
  return (
    <AuthProvider>
      <Layout>
        <Routes>
          {/* Public routes */}
          <Route path="/home" element={<Home />} />
          <Route path="/login" element={<Login />} />
          <Route path="/verify" element={<VerifyEmailPage />} />
          <Route path="/reset-password" element={<ResetPassword />} />
          
          {/* Protected routes */}
          <Route path="/dashboard" element={
            <ProtectedRoute>
              <Dashboard />
            </ProtectedRoute>
          } />
          <Route path="/security-dashboard" element={
            <ProtectedRoute>
              <SecurityDashboard />
            </ProtectedRoute>
          } />
          <Route path="/system-health" element={
            <ProtectedRoute>
              <SystemHealthDashboard />
            </ProtectedRoute>
          } />
          <Route path="/executive-dashboard" element={
            <ProtectedRoute>
              <ExecutiveDashboard />
            </ProtectedRoute>
          } />
          <Route path="/user-provisioning" element={
            <ProtectedRoute>
              <UserProvisioning />
            </ProtectedRoute>
          } />
          <Route path="/compliance" element={
            <ProtectedRoute>
              <ComplianceDashboard />
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
          
          {/* Conditional redirect based on authentication */}
          <Route path="/" element={<ConditionalRedirect />} />
          <Route path="*" element={<ConditionalRedirect />} />
        </Routes>
      </Layout>
    </AuthProvider>
  );
}

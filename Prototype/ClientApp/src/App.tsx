import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './contexts/AuthContext';
import { MigrationProvider } from './contexts/MigrationContext';
import { NotificationProvider } from './contexts/NotificationContext';
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
import AnalyticsOverview from './components/analytics/AnalyticsOverview';
import UserProvisioning from './components/provisioning/UserProvisioning';
import UserRequests from './components/user-requests/UserRequests';
import ComplianceDashboard from './components/compliance/ComplianceDashboard';
import Home from './components/home/Home';
import About from './components/pages/About';
import Contact from './components/pages/Contact';

// Import test runner for development
if (process.env.NODE_ENV === 'development') {
  import('./utils/testRunner');
}

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
      <MigrationProvider>
        <NotificationProvider>
          <Layout>
        <Routes>
          {/* Public routes */}
          <Route path="/home" element={<Home />} />
          <Route path="/about" element={<About />} />
          <Route path="/contact" element={<Contact />} />
          <Route path="/login" element={<Login />} />
          <Route path="/verify" element={<VerifyEmailPage />} />
          <Route path="/reset-password" element={<ResetPassword />} />
          
          {/* Protected routes */}
          <Route path="/dashboard" element={
            <ProtectedRoute  allowedRoles={['User', 'Admin', 'Platform Admin']}>
              <Dashboard />
            </ProtectedRoute>
          } />
          <Route path="/security-dashboard" element={
            <ProtectedRoute allowedRoles={['User', 'Admin', 'Platform Admin']}>
              <SecurityDashboard />
            </ProtectedRoute>
          } />
          <Route path="/system-health" element={
            <ProtectedRoute>
              <SystemHealthDashboard />
            </ProtectedRoute>
          } />
          <Route path="/analytics-overview" element={
            <ProtectedRoute allowedRoles={['User', 'Admin', 'Platform Admin']}>
              <AnalyticsOverview />
            </ProtectedRoute>
          } />
          <Route path="/user-provisioning" element={
            <ProtectedRoute allowedRoles={['User', 'Admin', 'Platform Admin']}>
              <UserProvisioning />
            </ProtectedRoute>
          } />
          <Route path="/user-requests" element={
            <ProtectedRoute>
              <UserRequests />
            </ProtectedRoute>
          } />
          <Route path="/compliance" element={
            <ProtectedRoute allowedRoles={['User', 'Admin', 'Platform Admin']}>
              <ComplianceDashboard />
            </ProtectedRoute>
          } />
          <Route path="/accounts" element={
            <ProtectedRoute allowedRoles={['User', 'Admin', 'Platform Admin']}>
              <Accounts />
            </ProtectedRoute>
          } />
          <Route path="/settings" element={
            <ProtectedRoute allowedRoles={['User', 'Admin', 'Platform Admin']}>
              <Settings />
            </ProtectedRoute>
          } />  
          <Route path="/applications" element={
            <ProtectedRoute allowedRoles={['User', 'Admin', 'Platform Admin']}>
              <Applications />
            </ProtectedRoute>
          } />
          <Route path="/audit-logs" element={
            <ProtectedRoute allowedRoles={['User', 'Admin', 'Platform Admin']}>
              <AuditLogs />
            </ProtectedRoute>
          } />
          <Route path="/activity-logs" element={
            <ProtectedRoute allowedRoles={['Platform Admin']}>
              <ActivityLogs />
            </ProtectedRoute>
          } />
          <Route path="/application-logs" element={
            <ProtectedRoute allowedRoles={['Platform Admin']}>
              <ApplicationLogs />
            </ProtectedRoute>
          } />
          <Route path="/roles" element={
            <ProtectedRoute allowedRoles={['Platform Admin']}>
              <Roles />
            </ProtectedRoute>
          } />
          <Route path="/system-health" element={
            <ProtectedRoute allowedRoles={['Platform Admin']}>
              <SystemHealthDashboard />
            </ProtectedRoute>
          } />
          
          {/* Conditional redirect based on authentication */}
          <Route path="/" element={<ConditionalRedirect />} />
          <Route path="*" element={<ConditionalRedirect />} />
        </Routes>
          </Layout>
        </NotificationProvider>
      </MigrationProvider>
    </AuthProvider>
  );
}

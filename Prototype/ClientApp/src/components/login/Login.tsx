import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { useForm, validationRules } from '../../hooks/useForm';
import { authApi } from '../../services/api';
import { Eye, EyeOff, Lock, User, AlertCircle, CheckCircle } from 'lucide-react';

interface LoginFormData {
  username: string;
  password: string;
}

interface RecoveryFormData {
  recoveryEmail: string;
  recoveryType: string;
}

const LoginForm: React.FC = () => {
  const [showRecovery, setShowRecovery] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [recoverySuccess, setRecoverySuccess] = useState(false);
  const { login, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const from = (location.state as any)?.from?.pathname || '/dashboard';

  // Redirect if already authenticated
  useEffect(() => {
    if (isAuthenticated) {
      navigate(from, { replace: true });
    }
  }, [isAuthenticated, navigate, from]);

  const loginForm = useForm<LoginFormData>({
    initialValues: {
      username: '',
      password: '',
    },
    validationRules: {
      username: validationRules.username,
      password: { required: true },
    },
    onSubmit: async (values) => {
      const result = await login(values);
      if (result.success) {
        navigate(from, { replace: true });
      } else {
        loginForm.setError('password', result.message);
      }
    },
  });

  const recoveryForm = useForm<RecoveryFormData>({
    initialValues: {
      recoveryEmail: '',
      recoveryType: 'PASSWORD',
    },
    validationRules: {
      recoveryEmail: validationRules.email,
      recoveryType: validationRules.required,
    },
    onSubmit: async (values) => {
      try {
        await authApi.forgotPassword(values.recoveryEmail, values.recoveryType);
        setRecoverySuccess(true);
        recoveryForm.reset();
        setTimeout(() => {
          setShowRecovery(false);
          setRecoverySuccess(false);
        }, 3000);
      } catch (error: any) {
        recoveryForm.setError('recoveryEmail', error.message || 'Recovery request failed');
      }
    },
  });


  return (
      <div className="vh-100 d-flex align-items-center justify-content-center bg-light overflow-hidden">
        <div className="container">
          <div className="row justify-content-center">
            <div className="col-md-6 col-lg-4">
              <div className="card shadow-lg border-0 rounded-4">
                <div className="card-body p-5">
                  <div className="text-center mb-4">
                    <h2 className="fw-bold text-dark mb-2">
                      Welcome Back
                    </h2>
                    <p className="text-muted">
                      Sign in to your account
                    </p>
                  </div>

                  <form onSubmit={loginForm.handleSubmit}>
                    <div className="mb-4">
                      <label htmlFor="username" className="form-label fw-semibold">
                        <User className="me-2" size={16} />
                        Username
                      </label>
                      <div className="position-relative">
                        <input
                            id="username"
                            name="username"
                            type="text"
                            value={loginForm.values.username}
                            onChange={(e) => loginForm.setValue('username', e.target.value)}
                            className={`form-control form-control-lg rounded-3 ${
                                loginForm.errors.username && loginForm.touched.username
                                    ? 'is-invalid'
                                    : ''
                            }`}
                            placeholder="Enter your username"
                        />
                        {loginForm.errors.username && loginForm.touched.username && (
                            <div className="invalid-feedback d-flex align-items-center">
                              <AlertCircle size={16} className="me-1" />
                              {loginForm.errors.username}
                            </div>
                        )}
                      </div>
                    </div>

                    <div className="mb-4">
                      <label htmlFor="password" className="form-label fw-semibold">
                        <Lock className="me-2" size={16} />
                        Password
                      </label>
                      <div className="position-relative">
                        <input
                            id="password"
                            name="password"
                            type={showPassword ? 'text' : 'password'}
                            value={loginForm.values.password}
                            onChange={(e) => loginForm.setValue('password', e.target.value)}
                            className={`form-control form-control-lg rounded-3 pe-5 ${
                                loginForm.errors.password && loginForm.touched.password
                                    ? 'is-invalid'
                                    : ''
                            }`}
                            placeholder="Enter your password"
                        />
                        <button
                            type="button"
                            onClick={() => setShowPassword(!showPassword)}
                            className="btn btn-outline-secondary position-absolute top-50 end-0 translate-middle-y me-2 border-0 p-1"
                            style={{ zIndex: 10 }}
                        >
                          {showPassword ? (
                              <EyeOff size={18} className="text-muted" />
                          ) : (
                              <Eye size={18} className="text-muted" />
                          )}
                        </button>
                        {loginForm.errors.password && loginForm.touched.password && (
                            <div className="invalid-feedback d-flex align-items-center">
                              <AlertCircle size={16} className="me-1" />
                              {loginForm.errors.password}
                            </div>
                        )}
                      </div>
                    </div>

                    <div className="d-grid mb-3">
                      <button
                          type="submit"
                          disabled={loginForm.isSubmitting || !loginForm.isValid}
                          className="btn btn-primary btn-lg rounded-3 fw-semibold"
                      >
                        {loginForm.isSubmitting ? (
                            <>
                              <div className="spinner-border spinner-border-sm me-2" role="status">
                                <span className="visually-hidden">Loading...</span>
                              </div>
                              Signing in...
                            </>
                        ) : (
                            'Sign In'
                        )}
                      </button>
                    </div>

                    <div className="text-center">
                      <button
                          type="button"
                          onClick={() => setShowRecovery(!showRecovery)}
                          className="btn btn-link text-decoration-none fw-semibold"
                      >
                        {showRecovery ? 'Cancel' : 'Forgot your password?'}
                      </button>
                    </div>
                  </form>

                  {showRecovery && (
                      <div className="mt-4 p-4 bg-light rounded-3 border">
                        <h5 className="fw-bold text-dark mb-3">Password Recovery</h5>

                        {recoverySuccess ? (
                            <div className="alert alert-success d-flex align-items-center rounded-3">
                              <CheckCircle size={20} className="text-success me-2" />
                              <span>
                          Recovery email sent successfully! Check your inbox.
                        </span>
                            </div>
                        ) : (
                            <form onSubmit={recoveryForm.handleSubmit}>
                              <div className="mb-3">
                                <label htmlFor="recoveryEmail" className="form-label fw-semibold">
                                  Email Address
                                </label>
                                <input
                                    id="recoveryEmail"
                                    name="recoveryEmail"
                                    type="email"
                                    value={recoveryForm.values.recoveryEmail}
                                    onChange={(e) => recoveryForm.setValue('recoveryEmail', e.target.value)}
                                    className={`form-control rounded-3 ${
                                        recoveryForm.errors.recoveryEmail && recoveryForm.touched.recoveryEmail
                                            ? 'is-invalid'
                                            : ''
                                    }`}
                                    placeholder="Enter your email"
                                />
                                {recoveryForm.errors.recoveryEmail && recoveryForm.touched.recoveryEmail && (
                                    <div className="invalid-feedback d-flex align-items-center">
                                      <AlertCircle size={16} className="me-1" />
                                      {recoveryForm.errors.recoveryEmail}
                                    </div>
                                )}
                              </div>

                              <div className="mb-3">
                                <label htmlFor="recoveryType" className="form-label fw-semibold">
                                  Recovery Type
                                </label>
                                <select
                                    id="recoveryType"
                                    name="recoveryType"
                                    value={recoveryForm.values.recoveryType}
                                    onChange={(e) => recoveryForm.setValue('recoveryType', e.target.value)}
                                    className="form-select rounded-3"
                                >
                                  <option value="PASSWORD">Password</option>
                                  <option value="USERNAME">Username</option>
                                </select>
                              </div>

                              <div className="d-grid">
                                <button
                                    type="submit"
                                    disabled={recoveryForm.isSubmitting || !recoveryForm.isValid}
                                    className="btn btn-warning rounded-3 fw-semibold"
                                >
                                  {recoveryForm.isSubmitting ? (
                                      <>
                                        <div className="spinner-border spinner-border-sm me-2" role="status">
                                          <span className="visually-hidden">Loading...</span>
                                        </div>
                                        Sending...
                                      </>
                                  ) : (
                                      'Send Recovery Email'
                                  )}
                                </button>
                              </div>
                            </form>
                        )}
                      </div>
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
  );
};

export default LoginForm;
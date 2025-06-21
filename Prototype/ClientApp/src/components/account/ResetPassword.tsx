import React, { useState, useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { Lock, Eye, EyeOff, AlertCircle, CheckCircle, Shield, ArrowLeft } from 'lucide-react';

const ResetPassword: React.FC = () => {
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();
    const token = searchParams.get('token');

    const [password, setPassword] = useState('');
    const [reTypePassword, setReTypePassword] = useState('');
    const [message, setMessage] = useState('');
    const [error, setError] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [showPassword, setShowPassword] = useState(false);
    const [showReTypePassword, setShowReTypePassword] = useState(false);

    useEffect(() => {
        if (!token) {
            setError('Invalid or expired password reset link.');
        }
    }, [token]);

    useEffect(() => {
        // Store original body styles
        const originalStyle = {
            overflow: document.body.style.overflow,
            height: document.body.style.height,
            position: document.body.style.position
        };

        // Prevent scrolling
        document.body.style.overflow = 'hidden';
        document.body.style.height = '100vh';
        document.body.style.position = 'fixed';
        document.body.style.width = '100%';

        // Cleanup on unmount
        return () => {
            document.body.style.overflow = originalStyle.overflow;
            document.body.style.height = originalStyle.height;
            document.body.style.position = originalStyle.position;
            document.body.style.width = '';
        };
    }, []);

    const validatePasswords = () => {
        const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/;
        
        if (!passwordRegex.test(password)) {
            setError('Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character');
            return false;
        }
        if (password !== reTypePassword) {
            setError('Passwords do not match.');
            return false;
        }
        return true;
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError('');

        if (!token) return;
        if (!validatePasswords()) return;

        setIsLoading(true);

        try {
            const response = await fetch('/PasswordReset', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    Token: token,
                    NewPassword: password,
                    ReTypePassword: reTypePassword
                }),
            });

            if (response.ok) {
                const result = await response.text();
                setMessage('Password reset successfully! You can now sign in with your new password.');
                setError('');
                // Redirect to login after 3 seconds
                setTimeout(() => {
                    navigate('/login');
                }, 3000);
            } else {
                const errorText = await response.text();
                setError(errorText || 'Failed to reset password. Please try again.');
                setMessage('');
            }
        } catch (err) {
            setError('An error occurred while resetting the password. Please try again.');
            setMessage('');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="vh-100 d-flex align-items-center justify-content-center bg-light overflow-hidden" style={{
            position: 'fixed',
            top: '0',
            left: '0',
            right: '0',
            bottom: '0',
            height: '100vh',
            width: '100vw',
            zIndex: 1
        }}>
            <div className="container">
                <div className="row justify-content-center">
                    <div className="col-md-6 col-lg-5">
                        <div className="card shadow-lg border-0 rounded-4 login-card">
                            <div className="card-body p-5">
                                <div className="text-center mb-4">
                                    <h2 className="fw-bold text-dark mb-2">
                                        Reset Password
                                    </h2>
                                    <p className="text-muted">
                                        Create a new secure password for your account
                                    </p>
                                </div>

                                {error && !token && (
                                    <div className="alert alert-danger d-flex align-items-center rounded-3 mb-4">
                                        <AlertCircle size={20} className="text-danger me-2" />
                                        <span>{error}</span>
                                    </div>
                                )}

                                {error && token && (
                                    <div className="alert alert-danger d-flex align-items-center rounded-3 mb-4">
                                        <AlertCircle size={20} className="text-danger me-2" />
                                        <span>{error}</span>
                                    </div>
                                )}

                                {message && (
                                    <div className="alert alert-success d-flex align-items-center rounded-3 mb-4">
                                        <CheckCircle size={20} className="text-success me-2" />
                                        <div>
                                            <div>{message}</div>
                                            <small className="text-muted">Redirecting to login page...</small>
                                        </div>
                                    </div>
                                )}

                                {!message && token && (
                                    <form onSubmit={handleSubmit}>
                                        <div className="mb-4">
                                            <label htmlFor="password" className="form-label fw-semibold">
                                                <Lock className="me-2" size={16} />
                                                New Password
                                            </label>
                                            <div className="position-relative">
                                                <input
                                                    id="password"
                                                    type={showPassword ? 'text' : 'password'}
                                                    value={password}
                                                    onChange={(e) => setPassword(e.target.value)}
                                                    className={`form-control form-control-lg rounded-3 pe-5 ${
                                                        error && error.includes('Password') ? 'is-invalid' : ''
                                                    }`}
                                                    placeholder="Enter your new password"
                                                    required
                                                    minLength={8}
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
                                            </div>
                                            <small className="text-muted">
                                                Password must be 8+ characters with uppercase, lowercase, digit, and special character (@$!%*?&)
                                            </small>
                                        </div>

                                        <div className="mb-4">
                                            <label htmlFor="reTypePassword" className="form-label fw-semibold">
                                                <Lock className="me-2" size={16} />
                                                Confirm New Password
                                            </label>
                                            <div className="position-relative">
                                                <input
                                                    id="reTypePassword"
                                                    type={showReTypePassword ? 'text' : 'password'}
                                                    value={reTypePassword}
                                                    onChange={(e) => setReTypePassword(e.target.value)}
                                                    className={`form-control form-control-lg rounded-3 pe-5 ${
                                                        error && error.includes('match') ? 'is-invalid' : ''
                                                    }`}
                                                    placeholder="Confirm your new password"
                                                    required
                                                />
                                                <button
                                                    type="button"
                                                    onClick={() => setShowReTypePassword(!showReTypePassword)}
                                                    className="btn btn-outline-secondary position-absolute top-50 end-0 translate-middle-y me-2 border-0 p-1"
                                                    style={{ zIndex: 10 }}
                                                >
                                                    {showReTypePassword ? (
                                                        <EyeOff size={18} className="text-muted" />
                                                    ) : (
                                                        <Eye size={18} className="text-muted" />
                                                    )}
                                                </button>
                                            </div>
                                        </div>

                                        <div className="d-grid mb-3">
                                            <button
                                                type="submit"
                                                disabled={isLoading || !password || !reTypePassword}
                                                className="btn btn-primary btn-lg rounded-3 fw-semibold"
                                            >
                                                {isLoading ? (
                                                    <>
                                                        <div className="spinner-border spinner-border-sm me-2" role="status">
                                                            <span className="visually-hidden">Loading...</span>
                                                        </div>
                                                        Resetting Password...
                                                    </>
                                                ) : (
                                                    'Reset Password'
                                                )}
                                            </button>
                                        </div>

                                        <div className="text-center">
                                            <button
                                                type="button"
                                                onClick={() => navigate('/login')}
                                                className="btn btn-link text-decoration-none fw-semibold"
                                            >
                                                <ArrowLeft size={16} className="me-1" />
                                                Back to Sign In
                                            </button>
                                        </div>
                                    </form>
                                )}

                                {!token && (
                                    <div className="text-center">
                                        <button
                                            type="button"
                                            onClick={() => navigate('/login')}
                                            className="btn btn-primary rounded-3 fw-semibold"
                                        >
                                            <ArrowLeft size={16} className="me-1" />
                                            Go to Sign In
                                        </button>
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

export default ResetPassword;
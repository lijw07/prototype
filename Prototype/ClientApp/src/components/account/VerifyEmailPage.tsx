import React, { useEffect, useRef, useState } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { Eye, EyeOff } from 'lucide-react';

interface User {
    userId: string;
    firstName: string;
    lastName: string;
    username: string;
    email: string;
    phoneNumber: string;
    isTemporary: boolean;
}

const VerifyEmailPage: React.FC = () => {
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();
    const hasRunRef = useRef(false);

    const [status, setStatus] = useState<'pending' | 'success' | 'error' | 'setPassword'>('pending');
    const [message, setMessage] = useState('');
    const [user, setUser] = useState<User | null>(null);
    const [passwordResetToken, setPasswordResetToken] = useState('');
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [passwordError, setPasswordError] = useState('');
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [showPassword, setShowPassword] = useState(false);
    const [showConfirmPassword, setShowConfirmPassword] = useState(false);

    useEffect(() => {
        if (hasRunRef.current) return;
        hasRunRef.current = true;

        const token = searchParams.get('token');
        console.log('Verification token from URL:', token);

        if (!token) {
            setStatus('error');
            setMessage('Invalid or missing token.');
            return;
        }

        const verifyEmail = async () => {
            try {
                const response = await fetch(`http://localhost:8080/VerifyUser?token=${encodeURIComponent(token)}`);

                const contentType = response.headers.get('content-type');
                let responseText = '';

                if (contentType?.includes('application/json')) {
                    const data = await response.json();
                    
                    if (response.ok && data.user && data.user.isTemporary) {
                        // This is a temporary user, show password setup
                        setStatus('setPassword');
                        setMessage(data.message || 'Please set your password to complete registration.');
                        setUser(data.user);
                        setPasswordResetToken(data.token);
                    } else if (response.ok) {
                        // Regular verification success
                        setStatus('success');
                        setMessage(data.message || 'Your email has been successfully verified!');
                    } else {
                        console.error('Verification failed response:', data);
                        setStatus('error');
                        setMessage(data.message || 'Verification failed.');
                    }
                } else {
                    responseText = await response.text();
                    if (response.ok) {
                        setStatus('success');
                        setMessage(responseText || 'Your email has been successfully verified!');
                    } else {
                        console.error('Verification failed response:', responseText);
                        setStatus('error');
                        setMessage(responseText || 'Verification failed.');
                    }
                }
            } catch (err) {
                console.error('Verification error:', err);
                setStatus('error');
                setMessage('An error occurred during verification.');
            }
        };

        verifyEmail();
    }, [searchParams]);

    const validatePassword = (password: string): string => {
        if (password.length < 8) {
            return 'Password must be at least 8 characters long.';
        }
        if (!/(?=.*[a-z])/.test(password)) {
            return 'Password must contain at least one lowercase letter.';
        }
        if (!/(?=.*[A-Z])/.test(password)) {
            return 'Password must contain at least one uppercase letter.';
        }
        if (!/(?=.*\d)/.test(password)) {
            return 'Password must contain at least one digit.';
        }
        if (!/(?=.*[@$!%*?&])/.test(password)) {
            return 'Password must contain at least one special character (@$!%*?&).';
        }
        return '';
    };

    const handlePasswordSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setPasswordError('');

        // Validate password
        const passwordValidationError = validatePassword(password);
        if (passwordValidationError) {
            setPasswordError(passwordValidationError);
            return;
        }

        // Check if passwords match
        if (password !== confirmPassword) {
            setPasswordError('Passwords do not match.');
            return;
        }

        setIsSubmitting(true);

        try {
            const response = await fetch('http://localhost:8080/PasswordReset', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    token: passwordResetToken,
                    newPassword: password,
                    reTypePassword: confirmPassword,
                }),
            });

            const data = await response.json();

            if (response.ok) {
                setStatus('success');
                setMessage('Email verified successfully! Your account has been created and you can now login.');
            } else {
                setPasswordError(data.message || 'Failed to set password.');
            }
        } catch (err) {
            console.error('Password reset error:', err);
            setPasswordError('An error occurred while setting your password.');
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light" style={{ 
            padding: '20px'
        }}>
            <div className="container">
                <div className="row justify-content-center">
                    <div className="col-lg-6 col-md-8">
                        <div className="card shadow-lg border-0" style={{ borderRadius: '20px' }}>
                            <div className="card-body p-5">
                                {/* Logo/Brand - Only show for pending and setPassword states */}
                                {(status === 'pending' || status === 'setPassword') && (
                                    <div className="text-center mb-4">
                                        <h4 className="fw-bold text-dark mb-1">Account Verification</h4>
                                    </div>
                                )}

                                {status === 'pending' && (
                                    <div className="text-center">
                                        <div className="spinner-border text-primary mb-3" role="status">
                                            <span className="visually-hidden">Loading...</span>
                                        </div>
                                        <p className="text-muted">Verifying your email address...</p>
                                    </div>
                                )}

                                {status === 'setPassword' && (
                                    <>
                                        {user && (
                                            <div className="text-center mb-4">
                                                <h5 className="fw-bold text-dark mb-2">Welcome, {user.firstName} {user.lastName}!</h5>
                                                <div className="bg-light rounded p-3 mb-3">
                                                    <div className="row text-start">
                                                        <div className="col-4 text-muted small">Username:</div>
                                                        <div className="col-8 fw-semibold">{user.username}</div>
                                                    </div>
                                                    <div className="row text-start">
                                                        <div className="col-4 text-muted small">Email:</div>
                                                        <div className="col-8 fw-semibold">{user.email}</div>
                                                    </div>
                                                </div>
                                                <p className="text-muted small mb-4">Please set your password to complete your account setup</p>
                                            </div>
                                        )}

                                        <form onSubmit={handlePasswordSubmit}>
                                            <div className="mb-4">
                                                <label htmlFor="password" className="form-label fw-semibold text-dark">New Password</label>
                                                <div className="position-relative">
                                                    <input
                                                        type={showPassword ? "text" : "password"}
                                                        className="form-control form-control-lg border-2"
                                                        id="password"
                                                        value={password}
                                                        onChange={(e) => setPassword(e.target.value)}
                                                        required
                                                        disabled={isSubmitting}
                                                        placeholder="Enter your new password"
                                                        style={{ borderRadius: '12px', paddingRight: '45px' }}
                                                    />
                                                    <button
                                                        type="button"
                                                        className="btn btn-outline-secondary position-absolute top-50 end-0 translate-middle-y me-2 border-0 p-1"
                                                        onClick={() => setShowPassword(!showPassword)}
                                                        disabled={isSubmitting}
                                                        style={{ zIndex: 10 }}
                                                    >
                                                        {showPassword ? (
                                                            <EyeOff size={18} className="text-muted" />
                                                        ) : (
                                                            <Eye size={18} className="text-muted" />
                                                        )}
                                                    </button>
                                                </div>
                                            </div>

                                            <div className="mb-4">
                                                <label htmlFor="confirmPassword" className="form-label fw-semibold text-dark">Confirm Password</label>
                                                <div className="position-relative">
                                                    <input
                                                        type={showConfirmPassword ? "text" : "password"}
                                                        className="form-control form-control-lg border-2"
                                                        id="confirmPassword"
                                                        value={confirmPassword}
                                                        onChange={(e) => setConfirmPassword(e.target.value)}
                                                        required
                                                        disabled={isSubmitting}
                                                        placeholder="Confirm your password"
                                                        style={{ borderRadius: '12px', paddingRight: '45px' }}
                                                    />
                                                    <button
                                                        type="button"
                                                        className="btn btn-outline-secondary position-absolute top-50 end-0 translate-middle-y me-2 border-0 p-1"
                                                        onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                                                        disabled={isSubmitting}
                                                        style={{ zIndex: 10 }}
                                                    >
                                                        {showConfirmPassword ? (
                                                            <EyeOff size={18} className="text-muted" />
                                                        ) : (
                                                            <Eye size={18} className="text-muted" />
                                                        )}
                                                    </button>
                                                </div>
                                            </div>

                                            {passwordError && (
                                                <div className="alert alert-danger border-0 mb-4" style={{ backgroundColor: '#f8d7da', borderRadius: '12px' }}>
                                                    <i className="bi bi-exclamation-circle-fill text-danger me-2"></i>
                                                    {passwordError}
                                                </div>
                                            )}

                                            <div className="d-grid mb-4">
                                                <button 
                                                    type="submit" 
                                                    className="btn btn-lg fw-semibold"
                                                    disabled={isSubmitting}
                                                    style={{ 
                                                        background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                                                        border: 'none',
                                                        borderRadius: '12px',
                                                        color: 'white'
                                                    }}
                                                >
                                                    {isSubmitting ? (
                                                        <>
                                                            <span className="spinner-border spinner-border-sm me-2" role="status"></span>
                                                            Setting Password...
                                                        </>
                                                    ) : (
                                                        <>
                                                            <i className="bi bi-key-fill me-2"></i>
                                                            Set Password
                                                        </>
                                                    )}
                                                </button>
                                            </div>
                                        </form>

                                        <div className="bg-light rounded p-3" style={{ borderRadius: '12px' }}>
                                            <h6 className="fw-semibold text-dark mb-2">
                                                <i className="bi bi-shield-check text-success me-2"></i>
                                                Password Requirements:
                                            </h6>
                                            <ul className="list-unstyled mb-0 small text-muted">
                                                <li className="mb-1">✓ At least 8 characters long</li>
                                                <li className="mb-1">✓ One uppercase letter (A-Z)</li>
                                                <li className="mb-1">✓ One lowercase letter (a-z)</li>
                                                <li className="mb-1">✓ One digit (0-9)</li>
                                                <li className="mb-0">✓ One special character (@$!%*?&)</li>
                                            </ul>
                                        </div>
                                    </>
                                )}

                                {status === 'success' && (
                                    <div className="text-center">
                                        <div className="mb-4">
                                            <div className="mx-auto mb-3" style={{ width: '80px', height: '80px' }}>
                                                <div className="rounded-circle bg-success d-flex align-items-center justify-content-center h-100">
                                                    <i className="bi bi-check-lg text-white" style={{ fontSize: '2.5rem' }}></i>
                                                </div>
                                            </div>
                                            <h4 className="fw-bold text-success mb-3">Success!</h4>
                                        </div>
                                        <div className="alert alert-success border-0 mb-4" style={{ backgroundColor: '#d4edda', borderRadius: '12px' }}>
                                            {message}
                                        </div>
                                        <button 
                                            className="btn btn-lg fw-semibold"
                                            onClick={() => navigate('/login')}
                                            style={{ 
                                                background: 'linear-gradient(135deg, #28a745 0%, #20c997 100%)',
                                                border: 'none',
                                                borderRadius: '12px',
                                                color: 'white',
                                                padding: '12px 40px'
                                            }}
                                        >
                                            <i className="bi bi-box-arrow-in-right me-2"></i>
                                            Go to Login
                                        </button>
                                    </div>
                                )}

                                {status === 'error' && (
                                    <div className="text-center">
                                        <div className="mb-4">
                                            <div className="mx-auto mb-3" style={{ width: '80px', height: '80px' }}>
                                                <div className="rounded-circle bg-danger d-flex align-items-center justify-content-center h-100">
                                                    <i className="bi bi-x-lg text-white" style={{ fontSize: '2.5rem' }}></i>
                                                </div>
                                            </div>
                                            <h4 className="fw-bold text-danger mb-3">Verification Failed</h4>
                                        </div>
                                        <div className="alert alert-danger border-0 mb-4" style={{ backgroundColor: '#f8d7da', borderRadius: '12px' }}>
                                            {message}
                                        </div>
                                        <button 
                                            className="btn btn-lg fw-semibold"
                                            onClick={() => navigate('/login')}
                                            style={{ 
                                                background: 'linear-gradient(135deg, #6c757d 0%, #495057 100%)',
                                                border: 'none',
                                                borderRadius: '12px',
                                                color: 'white',
                                                padding: '12px 40px'
                                            }}
                                        >
                                            <i className="bi bi-arrow-left me-2"></i>
                                            Back to Login
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

export default VerifyEmailPage;
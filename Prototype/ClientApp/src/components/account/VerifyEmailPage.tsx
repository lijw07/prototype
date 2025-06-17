import React, { useEffect, useState } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';

const VerifyEmailPage: React.FC = () => {
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();

    const [status, setStatus] = useState<'pending' | 'success' | 'error'>('pending');
    const [message, setMessage] = useState('');

    useEffect(() => {
        const token = searchParams.get('token');

        if (!token) {
            setStatus('error');
            setMessage('Invalid or missing token.');
            return;
        }

        const verifyEmail = async () => {
            try {
                const response = await fetch(`/VerifyUser?token=${encodeURIComponent(token)}`);

                if (response.ok) {
                    const msg = await response.text();
                    setStatus('success');
                    setMessage(msg || 'Your email has been successfully verified!');

                    setTimeout(() => {
                        navigate('/login');
                    }, 5000); // auto-redirect in 5 seconds
                } else {
                    const errText = await response.text();
                    setStatus('error');
                    setMessage(errText || 'Verification failed.');
                }
            } catch (err) {
                console.error('Verification error:', err);
                setStatus('error');
                setMessage('An error occurred during verification.');
            }
        };

        verifyEmail();
    }, [searchParams, navigate]);

    const handleLoginRedirect = () => {
        navigate('/login');
    };

    return (
        <div className="container mt-5">
            <div className="card shadow p-4">
                <div className="card-body text-center">
                    {status === 'pending' && <p>Verifying your email...</p>}

                    {status === 'success' && (
                        <>
                            <div className="alert alert-success">{message}</div>
                            <p>You will be redirected to login shortly.</p>
                            <button className="btn btn-primary mt-3" onClick={handleLoginRedirect}>
                                Go to Login
                            </button>
                        </>
                    )}

                    {status === 'error' && (
                        <>
                            <div className="alert alert-danger">{message}</div>
                            <button className="btn btn-secondary mt-3" onClick={handleLoginRedirect}>
                                Back to Login
                            </button>
                        </>
                    )}
                </div>
            </div>
        </div>
    );
};

export default VerifyEmailPage;
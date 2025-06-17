import React, { useEffect, useRef, useState } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';

const VerifyEmailPage: React.FC = () => {
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();
    const hasRunRef = useRef(false);

    const [status, setStatus] = useState<'pending' | 'success' | 'error'>('pending');
    const [message, setMessage] = useState('');

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
                const response = await fetch(`/VerifyUser?token=${encodeURIComponent(token)}`);

                const contentType = response.headers.get('content-type');
                let responseText = '';

                if (contentType?.includes('application/json')) {
                    const data = await response.json();
                    responseText = data.message || '';
                } else {
                    responseText = await response.text();
                }

                if (response.ok) {
                    setStatus('success');
                    setMessage(responseText || 'Your email has been successfully verified!');
                } else {
                    console.error('Verification failed response:', responseText);
                    setStatus('error');
                    setMessage(responseText || 'Verification failed.');
                }
            } catch (err) {
                console.error('Verification error:', err);
                setStatus('error');
                setMessage('An error occurred during verification.');
            }
        };

        verifyEmail();
    }, [searchParams]);

    return (
        <div className="container mt-5">
            <div className="card shadow p-4">
                <div className="card-body text-center">
                    {status === 'pending' && <p>Verifying your email...</p>}

                    {status === 'success' && (
                        <>
                            <div className="alert alert-success">{message}</div>
                            <button className="btn btn-primary mt-3" onClick={() => navigate('/login')}>
                                Go to Login
                            </button>
                        </>
                    )}

                    {status === 'error' && (
                        <>
                            <div className="alert alert-danger">{message}</div>
                            <button className="btn btn-secondary mt-3" onClick={() => navigate('/login')}>
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
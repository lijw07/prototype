import React, { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';

const ResetPassword: React.FC = () => {
    const [searchParams] = useSearchParams();
    const token = searchParams.get('token');

    const [password, setPassword] = useState('');
    const [reTypePassword, setReTypePassword] = useState('');
    const [message, setMessage] = useState('');
    const [error, setError] = useState('');

    useEffect(() => {
        if (!token) {
            setError('Invalid or expired password reset link.');
        }
    }, [token]);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        if (!token) return;

        try {
            const response = await fetch('/PasswordReset', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    token,
                    password,
                    reTypePassword
                }),
            });

            if (response.ok) {
                const result = await response.text();
                setMessage(result);
                setError('');
            } else {
                const errorText = await response.text();
                setError(errorText);
                setMessage('');
            }
        } catch (err) {
            setError('An error occurred while resetting the password.');
        }
    };

    return (
        <div className="container">
            <h2>Reset Password</h2>
    {error && <p style={{ color: 'red' }}>{error}</p>}
        {message && <p style={{ color: 'green' }}>{message}</p>}
            {!message && (
                <form onSubmit={handleSubmit}>
                    <div>
                        <label>New Password:</label>
            <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                />
                </div>
                <div>
                <label>Re-Type Password:</label>
            <input
                type="password"
                value={reTypePassword}
                onChange={(e) => setReTypePassword(e.target.value)}
                required
                />
                </div>
                <button type="submit">Reset Password</button>
            </form>
            )}
            </div>
        );
        };

        export default ResetPassword;
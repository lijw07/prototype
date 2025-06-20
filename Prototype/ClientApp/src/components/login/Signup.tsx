import React, { useState, FormEvent, ChangeEvent } from 'react';
import { authApi } from '../../services/api';

const RegisterForm: React.FC = () => {
    const [formData, setFormData] = useState({
        firstName: '',
        lastName: '',
        username: '',
        email: '',
        phoneNumber: '',
        password: '',
        reEnterPassword: '',
    });

    const [error, setError] = useState('');
    const [success, setSuccess] = useState('');

    const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setFormData(prev => ({ ...prev, [name]: value }));
    };

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setError('');
        setSuccess('');

        const {
            firstName,
            lastName,
            username,
            email,
            phoneNumber,
            password,
            reEnterPassword,
        } = formData;

        if (password !== reEnterPassword) {
            setError('Passwords do not match.');
            return;
        }

        try {
            const payload = {
                firstName,
                lastName,
                username,
                email,
                phoneNumber,
                password,
                reEnterPassword,
            };

            console.log("Sending register payload:", payload);

            const data = await authApi.register(payload);

            setSuccess(data.message || 'Registration successful!');
            
        } catch (err: any) {
            console.error('Registration error:', err);
            setError(err.message || 'Registration failed.');
        }
    };

    return (
        <form onSubmit={handleSubmit}>
            <h2 className="mb-4 text-center">Register</h2>

            <input name="firstName" placeholder="First Name" value={formData.firstName} onChange={handleChange} className="form-control mb-2" required />
            <input name="lastName" placeholder="Last Name" value={formData.lastName} onChange={handleChange} className="form-control mb-2" required />
            <input name="username" placeholder="Username" value={formData.username} onChange={handleChange} className="form-control mb-2" required />
            <input name="email" type="email" placeholder="Email" value={formData.email} onChange={handleChange} className="form-control mb-2" required />
            <input name="phoneNumber" type="tel" placeholder="Phone Number" value={formData.phoneNumber} onChange={handleChange} className="form-control mb-2" required />
            <input name="password" type="password" placeholder="Password" value={formData.password} onChange={handleChange} className="form-control mb-2" required />
            <input name="reEnterPassword" type="password" placeholder="Re-enter Password" value={formData.reEnterPassword} onChange={handleChange} className="form-control mb-3" required />

            {error && <div className="alert alert-danger">{error}</div>}
            {success && <div className="alert alert-success">{success}</div>}

            <button type="submit" className="btn btn-primary w-100">Register</button>

            <div className="text-center mt-3">
                Already registered? <a href="/login">Sign in</a>
            </div>
        </form>
    );
};

export default RegisterForm;
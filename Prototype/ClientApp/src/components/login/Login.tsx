import React, { Component, ChangeEvent, FormEvent } from 'react';

interface LoginFormState {
  username: string;
  password: string;
  error: string;
  recoveryEmail: string;
  recoveryType: string;
  showRecovery: boolean;
}

interface LoginFormProps {}

class LoginForm extends Component<LoginFormProps, LoginFormState> {
  constructor(props: LoginFormProps) {
    super(props);
    this.state = {
      username: '',
      password: '',
      error: '',
      recoveryEmail: '',
      recoveryType: 'PASSWORD',
      showRecovery: false,
    };
  }

  handleChange = (e: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    this.setState(prevState => ({
      ...prevState,
      [name]: value
    }));
  };

  handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const { username, password } = this.state;

    try {
      const response = await fetch('/login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ username, password }),
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Login failed');
      }

      const data = await response.json();
      localStorage.setItem('authToken', data.token);
      window.location.href = '/settings';
    } catch (error: unknown) {
      const errorMsg = error instanceof Error ? error.message : 'Unknown error occurred';
      this.setState({ error: errorMsg });
    }
  };

  handleForgotSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const { recoveryEmail, recoveryType } = this.state;

    console.log('Sending ForgotUser request with payload:', {
      email: recoveryEmail,
      userRecoveryType: recoveryType,
    });

    try {
      const response = await fetch('/ForgotUser', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          email: recoveryEmail,
          userRecoveryType: recoveryType,
        }),
      });

      if (!response.ok) {
        const err = await response.json();
        throw new Error(err.message || 'Request failed');
      }

      alert('Recovery email sent successfully!');
      this.setState({ recoveryEmail: '', showRecovery: false });
    } catch (error: unknown) {
      const errorMsg = error instanceof Error ? error.message : 'Unknown error occurred';
      this.setState({ error: errorMsg });
    }
  };

  toggleRecoveryForm = () => {
    this.setState(prev => ({ showRecovery: !prev.showRecovery }));
  };

  handleCreateAccount = () => {
    window.location.href = '/sign-up';
  };

  render() {
    const { username, password, error, showRecovery, recoveryEmail, recoveryType } = this.state;

    return (
        <div className="container mt-5" style={{ maxWidth: '500px' }}>
          <h2 className="mb-4 text-center">Login</h2>
          <form onSubmit={this.handleSubmit}>
            <div className="mb-3">
              <label>Username</label>
              <input
                  name="username"
                  type="text"
                  className="form-control"
                  placeholder="Enter username"
                  value={username}
                  onChange={this.handleChange}
              />
            </div>

            <div className="mb-3">
              <label>Password</label>
              <input
                  name="password"
                  type="password"
                  className="form-control"
                  placeholder="Enter password"
                  value={password}
                  onChange={this.handleChange}
              />
            </div>

            {error && <div className="alert alert-danger">{error}</div>}

            <div className="d-grid mb-2">
              <button type="submit" className="btn btn-primary">
                Login
              </button>
            </div>

            <div className="d-grid mb-2">
              <button type="button" className="btn btn-outline-secondary" onClick={this.handleCreateAccount}>
                Create Account
              </button>
            </div>

            <div className="text-center">
              <button type="button" className="btn btn-link" onClick={this.toggleRecoveryForm}>
                {showRecovery ? 'Cancel' : 'Forgot Password?'}
              </button>
            </div>
          </form>

          {showRecovery && (
              <form className="mt-4" onSubmit={this.handleForgotSubmit}>
                <h5>Password Recovery</h5>
                <div className="mb-3">
                  <label>Email</label>
                  <input
                      name="recoveryEmail"
                      type="email"
                      className="form-control"
                      placeholder="Enter your email"
                      value={recoveryEmail}
                      onChange={this.handleChange}
                      required
                  />
                </div>

                <div className="mb-3">
                  <label>Recovery Type</label>
                  <select
                      name="recoveryType"
                      className="form-control"
                      value={recoveryType}
                      onChange={this.handleChange}
                  >
                    <option value="PASSWORD">Password</option>
                    <option value="USERNAME">Username</option>
                  </select>
                </div>

                <div className="d-grid">
                  <button type="submit" className="btn btn-warning">
                    Send Recovery Email
                  </button>
                </div>
              </form>
          )}
        </div>
    );
  }
}

export default LoginForm;